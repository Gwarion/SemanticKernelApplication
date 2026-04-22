namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Represents the current result state of an agent execution.
/// </summary>
public enum AgentExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
