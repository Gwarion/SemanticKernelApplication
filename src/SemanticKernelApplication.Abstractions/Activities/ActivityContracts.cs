namespace SemanticKernelApplication.Abstractions.Activities;

public enum ActivityKind
{
    Session,
    Workflow,
    Turn,
    Message,
    AgentExecution,
    Coordination,
    Status,
    Log,
    Metric
}

public enum ActivityStatus
{
    Pending,
    Running,
    Streaming,
    Completed,
    Failed,
    Cancelled
}

public enum ActivitySeverity
{
    Trace,
    Information,
    Warning,
    Error
}

public sealed record ActivityLogEntry(
    long Sequence,
    ActivityKind Kind,
    ActivityStatus Status,
    ActivitySeverity Severity,
    string Title,
    string Message,
    DateTimeOffset TimestampUtc,
    string? SessionId = null,
    string? ConversationId = null,
    string? AgentId = null,
    string? TurnId = null,
    string? CorrelationId = null,
    string? Delta = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ActivityStreamEnvelope(
    string StreamId,
    ActivityLogEntry Entry);

public sealed record UsageSnapshot(
    int InputTokens = 0,
    int OutputTokens = 0,
    int TotalTokens = 0,
    TimeSpan? Duration = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public interface IActivitySink
{
    ValueTask PublishAsync(ActivityStreamEnvelope envelope, CancellationToken cancellationToken = default);
}

public interface IActivityStreamReader
{
    IAsyncEnumerable<ActivityStreamEnvelope> ReadAllAsync(CancellationToken cancellationToken = default);
}
