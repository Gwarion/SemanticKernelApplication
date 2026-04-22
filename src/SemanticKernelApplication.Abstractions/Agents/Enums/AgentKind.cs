namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Describes the broad role an agent plays in the workbench.
/// </summary>
public enum AgentKind
{
    Assistant,
    Coordinator,
    UserDefined,
    ToolProxy
}
