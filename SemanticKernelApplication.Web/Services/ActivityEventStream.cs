using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace SemanticKernelApplication.Web.Services;

public sealed class ActivityEventStream : IActivityEventStream
{
    private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers = new();
    private readonly object _historyLock = new();
    private readonly List<ActivityEventEnvelope> _history = [];
    private long _sequence;

    public ActivityEventStream(int maxHistory = 256)
    {
        if (maxHistory <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHistory));
        }

        MaxHistory = maxHistory;
    }

    public int MaxHistory { get; }

    public ValueTask<ActivityEventEnvelope> PublishAsync(ActivityEvent activityEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activityEvent);

        cancellationToken.ThrowIfCancellationRequested();

        var envelope = new ActivityEventEnvelope(
            Sequence: Interlocked.Increment(ref _sequence),
            Timestamp: DateTimeOffset.UtcNow,
            Type: activityEvent.Type,
            Source: activityEvent.Source,
            ActivityId: activityEvent.ActivityId,
            Stream: activityEvent.Stream,
            Message: activityEvent.Message,
            Status: activityEvent.Status,
            Data: activityEvent.Data);

        AddToHistory(envelope);

        foreach (var subscriber in _subscribers.Values)
        {
            if (!subscriber.Matches(envelope))
            {
                continue;
            }

            subscriber.Writer.TryWrite(envelope);
        }

        return ValueTask.FromResult(envelope);
    }

    public IAsyncEnumerable<ActivityEventEnvelope> SubscribeAsync(
        string? stream = null,
        int replayCount = 0,
        CancellationToken cancellationToken = default)
    {
        replayCount = Math.Max(0, replayCount);

        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<ActivityEventEnvelope>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var subscriber = new Subscriber(stream, channel);
        _subscribers[subscriptionId] = subscriber;

        return ReadSubscription(subscriptionId, subscriber, replayCount, cancellationToken);
    }

    public IReadOnlyList<ActivityEventEnvelope> GetRecentEvents(string? stream = null, int count = 50)
    {
        count = Math.Max(0, count);
        if (count == 0)
        {
            return [];
        }

        lock (_historyLock)
        {
            return _history
                .Where(eventEnvelope => MatchesStream(eventEnvelope, stream))
                .TakeLast(count)
                .ToArray();
        }
    }

    private async IAsyncEnumerable<ActivityEventEnvelope> ReadSubscription(
        Guid subscriptionId,
        Subscriber subscriber,
        int replayCount,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            foreach (var envelope in GetRecentEvents(subscriber.Stream, replayCount))
            {
                yield return envelope;
            }

            await foreach (var envelope in subscriber.Reader.ReadAllAsync(cancellationToken))
            {
                yield return envelope;
            }
        }
        finally
        {
            _subscribers.TryRemove(subscriptionId, out _);
            subscriber.Writer.TryComplete();
        }
    }

    private void AddToHistory(ActivityEventEnvelope envelope)
    {
        lock (_historyLock)
        {
            _history.Add(envelope);

            if (_history.Count > MaxHistory)
            {
                _history.RemoveRange(0, _history.Count - MaxHistory);
            }
        }
    }

    private static bool MatchesStream(ActivityEventEnvelope envelope, string? stream) =>
        string.IsNullOrWhiteSpace(stream) ||
        string.Equals(envelope.Stream, stream, StringComparison.OrdinalIgnoreCase);

    private sealed record Subscriber(string? Stream, Channel<ActivityEventEnvelope> Channel)
    {
        public ChannelWriter<ActivityEventEnvelope> Writer => Channel.Writer;

        public ChannelReader<ActivityEventEnvelope> Reader => Channel.Reader;

        public bool Matches(ActivityEventEnvelope envelope) => MatchesStream(envelope, Stream);
    }
}
