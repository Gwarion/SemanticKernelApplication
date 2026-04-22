namespace SemanticKernelApplication.Abstractions.Workbench;

/// <summary>
/// Represents the current persisted provider and model configuration.
/// </summary>
/// <param name="SelectedProviderId">Identifier of the selected provider.</param>
/// <param name="SelectedModelId">Identifier of the selected model.</param>
/// <param name="ApiKeyConfigured">Indicates whether an API key is stored for the selected provider.</param>
/// <param name="ApiKey">Optional stored API key.</param>
public sealed record GlobalModelConfiguration(
    string SelectedProviderId,
    string SelectedModelId,
    bool ApiKeyConfigured,
    string? ApiKey = null);
