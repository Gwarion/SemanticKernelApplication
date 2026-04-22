using SemanticKernelApplication.Abstractions.Providers;

namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Represents one provider entry persisted in the local provider catalog.
/// </summary>
public sealed class AgentProviderRegistration
{
    /// <summary>
    /// Gets the unique provider identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name shown in the UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the provider family.
    /// </summary>
    public AiProviderKind Kind { get; init; }

    /// <summary>
    /// Gets an optional custom endpoint for compatible providers.
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>
    /// Gets an optional organization identifier used by the provider.
    /// </summary>
    public string? OrganizationId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the provider is the default catalog choice.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Gets the list of models exposed by the provider.
    /// </summary>
    public IReadOnlyList<AgentModelRegistration> Models { get; init; } = [];
}
