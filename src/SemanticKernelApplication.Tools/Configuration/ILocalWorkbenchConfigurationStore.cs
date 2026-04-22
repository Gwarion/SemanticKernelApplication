using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Persists local workbench configuration such as providers, defaults, API keys, and known workspaces.
/// </summary>
public interface ILocalWorkbenchConfigurationStore
{
    /// <summary>
    /// Returns the locally registered provider catalog.
    /// </summary>
    IReadOnlyList<AgentProviderRegistration> GetProviders();

    /// <summary>
    /// Returns a provider registration by identifier.
    /// </summary>
    /// <param name="providerId">Provider identifier to resolve.</param>
    AgentProviderRegistration? GetProvider(string? providerId);

    /// <summary>
    /// Returns the saved workspace paths known to the application.
    /// </summary>
    IReadOnlyList<string> GetKnownWorkspacePaths();

    /// <summary>
    /// Returns the persisted global provider/model configuration.
    /// </summary>
    GlobalModelConfiguration GetGlobalModelConfiguration();

    /// <summary>
    /// Returns the stored API key for a provider, when available.
    /// </summary>
    /// <param name="providerId">Provider identifier to inspect.</param>
    string? GetApiKey(string? providerId);

    /// <summary>
    /// Returns the current persisted workspace path.
    /// </summary>
    string GetWorkspacePath();

    /// <summary>
    /// Persists a workspace path and makes it the current selection.
    /// </summary>
    /// <param name="workspacePath">Workspace path to persist.</param>
    string SetWorkspacePath(string workspacePath);

    /// <summary>
    /// Persists a new global provider/model configuration.
    /// </summary>
    /// <param name="request">Configuration request to store.</param>
    GlobalModelConfiguration UpdateGlobalModelConfiguration(GlobalModelConfigurationRequest request);
}
