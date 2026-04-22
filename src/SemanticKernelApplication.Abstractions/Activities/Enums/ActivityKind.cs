namespace SemanticKernelApplication.Abstractions.Activities;

/// <summary>
/// Identifies the kind of runtime activity being recorded.
/// </summary>
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
