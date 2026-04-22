namespace SemanticKernelApplication.Abstractions.Workbench;

/// <summary>
/// Requests an update to the persisted provider and model defaults.
/// </summary>
/// <param name="SelectedProviderId">Provider to persist.</param>
/// <param name="SelectedModelId">Model to persist.</param>
/// <param name="ApiKey">Optional API key to save for the provider.</param>
public sealed record GlobalModelConfigurationRequest(
    string SelectedProviderId,
    string SelectedModelId,
    string? ApiKey = null);
