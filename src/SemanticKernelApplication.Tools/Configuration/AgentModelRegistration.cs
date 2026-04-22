namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Represents one model entry inside a provider registration.
/// </summary>
public sealed class AgentModelRegistration
{
    /// <summary>
    /// Gets the provider-specific model identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name shown in the UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the model is the provider default.
    /// </summary>
    public bool IsDefault { get; init; }
}
