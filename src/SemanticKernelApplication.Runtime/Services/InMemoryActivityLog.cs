using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using SemanticKernelApplication.Abstractions.Activities;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class InMemoryActivityLog : IActivitySink, IActivityStreamReader
{
    private readonly ConcurrentQueue<ActivityStreamEnvelope> _history = new();
    private readonly Channel<ActivityStreamEnvelope> _channel = Channel.CreateUnbounded<ActivityStreamEnvelope>();
    private long _sequence;

    public IReadOnlyList<ActivityLogEntry> GetRecent(int count = 100)
    {
        return _history
            .TakeLast(Math.Max(0, count))
            .Select(envelope => envelope.Entry)
            .ToArray();
    }

    public async ValueTask PublishAsync(ActivityStreamEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var entry = envelope.Entry
            .ToBuilder()
            .WithSequence(Interlocked.Increment(ref _sequence))
            .WithTimestampUtc(envelope.Entry.TimestampUtc == default ? DateTimeOffset.UtcNow : envelope.Entry.TimestampUtc)
            .Build();

        var normalized = envelope with { Entry = entry };
        _history.Enqueue(normalized);

        while (_history.Count > 500 && _history.TryDequeue(out _))
        {
        }

        await _channel.Writer.WriteAsync(normalized, cancellationToken);
    }

    public async IAsyncEnumerable<ActivityStreamEnvelope> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var envelope in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return envelope;
        }
    }
}
