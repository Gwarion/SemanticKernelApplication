using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

/// <summary>
/// Resolves provider definitions and registrations from the local catalog.
/// </summary>
public interface IAiProviderCatalog
{
    /// <summary>
    /// Returns all providers currently available to the workbench.
    /// </summary>
    IReadOnlyList<ModelProviderDefinition> GetProviders();

    /// <summary>
    /// Returns a provider definition by identifier, or the selected provider when no identifier is supplied.
    /// </summary>
    /// <param name="providerId">Optional provider identifier.</param>
    ModelProviderDefinition? GetProvider(string? providerId);

    /// <summary>
    /// Returns the underlying provider registration used to configure runtime connectors.
    /// </summary>
    /// <param name="providerId">Optional provider identifier.</param>
    AgentProviderRegistration? GetRegistration(string? providerId);
}
