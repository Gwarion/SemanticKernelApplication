namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Provides persistence operations for local workbench configuration data.
/// </summary>
public interface ILocalWorkbenchConfigurationRepository
{
    /// <summary>
    /// Loads the persisted local configuration, creating defaults when needed.
    /// </summary>
    LocalWorkbenchConfigurationRecord Load(string defaultWorkspacePath);

    /// <summary>
    /// Persists the current workspace selection and known-workspace history.
    /// </summary>
    LocalWorkbenchConfigurationRecord SaveWorkspace(LocalWorkbenchConfigurationRecord current, string workspacePath);

    /// <summary>
    /// Persists the global provider, model, and API key selection.
    /// </summary>
    LocalWorkbenchConfigurationRecord SaveGlobalModelConfiguration(
        LocalWorkbenchConfigurationRecord current,
        string selectedProviderId,
        string selectedModelId,
        string? apiKey);
}
