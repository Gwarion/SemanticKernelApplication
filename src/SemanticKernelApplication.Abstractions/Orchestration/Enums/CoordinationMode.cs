namespace SemanticKernelApplication.Abstractions.Orchestration;

/// <summary>
/// Defines how the coordinator should route work across agents.
/// </summary>
public enum CoordinationMode
{
    Sequential,
    RoundRobin,
    Facilitated,
    Custom
}
