namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Describes a named capability exposed by an agent.
/// </summary>
/// <param name="Name">Capability identifier.</param>
/// <param name="Description">Human-readable explanation of the capability.</param>
/// <param name="IsEnabled">Indicates whether the capability is active.</param>
/// <param name="Metadata">Optional provider- or app-specific metadata.</param>
public sealed record AgentCapability(
    string Name,
    string Description,
    bool IsEnabled = true,
    IReadOnlyDictionary<string, string>? Metadata = null);
