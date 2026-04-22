namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Represents the minimal agent identity needed during orchestration and execution.
/// </summary>
/// <param name="AgentId">Unique identifier of the referenced agent.</param>
/// <param name="DisplayName">Name shown in coordinator output.</param>
/// <param name="Kind">Broad classification of the agent.</param>
/// <param name="Metadata">Optional contextual metadata.</param>
public sealed record AgentReference(
    string AgentId,
    string DisplayName,
    AgentKind Kind,
    IReadOnlyDictionary<string, string>? Metadata = null);
