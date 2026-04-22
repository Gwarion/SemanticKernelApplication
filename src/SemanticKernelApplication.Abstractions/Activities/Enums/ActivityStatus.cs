namespace SemanticKernelApplication.Abstractions.Activities;

/// <summary>
/// Describes the execution state of a runtime activity.
/// </summary>
public enum ActivityStatus
{
    Pending,
    Running,
    Streaming,
    Completed,
    Failed,
    Cancelled
}
