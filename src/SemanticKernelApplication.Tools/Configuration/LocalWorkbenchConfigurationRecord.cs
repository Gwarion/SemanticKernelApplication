using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Represents the full persisted local configuration snapshot loaded from storage.
/// </summary>
public sealed record LocalWorkbenchConfigurationRecord(
    IReadOnlyList<AgentProviderRegistration> Providers,
    IReadOnlyList<string> KnownWorkspacePaths,
    string WorkspacePath,
    string SelectedProviderId,
    string SelectedModelId,
    IReadOnlyDictionary<string, string> ApiKeys)
{
    /// <summary>
    /// Resolves the effective provider by identifier, falling back to the selected provider.
    /// </summary>
    public AgentProviderRegistration? GetProvider(string? providerId)
    {
        var effectiveId = string.IsNullOrWhiteSpace(providerId) ? SelectedProviderId : providerId.Trim();
        return Providers.FirstOrDefault(provider => string.Equals(provider.Id, effectiveId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns the current global model configuration view.
    /// </summary>
    public GlobalModelConfiguration ToGlobalModelConfiguration() =>
        new(
            SelectedProviderId,
            SelectedModelId,
            ApiKeys.TryGetValue(SelectedProviderId, out var apiKey) && !string.IsNullOrWhiteSpace(apiKey),
            ApiKeys.GetValueOrDefault(SelectedProviderId));
}
