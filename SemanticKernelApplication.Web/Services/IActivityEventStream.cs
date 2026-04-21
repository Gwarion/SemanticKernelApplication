namespace SemanticKernelApplication.Web.Services;

public interface IActivityEventStream
{
    ValueTask<ActivityEventEnvelope> PublishAsync(ActivityEvent activityEvent, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ActivityEventEnvelope> SubscribeAsync(
        string? stream = null,
        int replayCount = 0,
        CancellationToken cancellationToken = default);

    IReadOnlyList<ActivityEventEnvelope> GetRecentEvents(string? stream = null, int count = 50);
}
