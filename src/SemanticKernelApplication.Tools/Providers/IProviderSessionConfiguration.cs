using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

/// <summary>
/// Tracks the provider and model selection currently active for runtime execution.
/// </summary>
public interface IProviderSessionConfiguration
{
    /// <summary>
    /// Gets the currently selected provider identifier.
    /// </summary>
    string? SelectedProviderId { get; }

    /// <summary>
    /// Gets the currently selected model identifier.
    /// </summary>
    string? SelectedModelId { get; }

    /// <summary>
    /// Gets the API key currently associated with the selected provider.
    /// </summary>
    string? ApiKey { get; }

    /// <summary>
    /// Returns the current provider/model configuration snapshot.
    /// </summary>
    GlobalModelConfiguration GetConfiguration();

    /// <summary>
    /// Applies a new provider/model configuration and returns the updated snapshot.
    /// </summary>
    /// <param name="request">Configuration change to apply.</param>
    GlobalModelConfiguration Update(GlobalModelConfigurationRequest request);

    /// <summary>
    /// Resolves the selected provider registration for runtime connector setup.
    /// </summary>
    AgentProviderRegistration? ResolveSelectedProvider();
}
