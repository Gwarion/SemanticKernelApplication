using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Holds the in-memory local configuration state loaded from persistence.
/// </summary>
internal sealed class LocalWorkbenchConfigurationState
{
    public List<AgentProviderRegistration> Providers { get; set; } = [];

    public List<string> KnownWorkspacePaths { get; set; } = [];

    public string WorkspacePath { get; set; } = string.Empty;

    public string SelectedProviderId { get; set; } = string.Empty;

    public string SelectedModelId { get; set; } = string.Empty;

    public Dictionary<string, string> ApiKeys { get; } = new(StringComparer.OrdinalIgnoreCase);

    public AgentProviderRegistration? GetProvider(string? providerId)
    {
        var effectiveId = string.IsNullOrWhiteSpace(providerId) ? SelectedProviderId : providerId.Trim();
        return Providers.FirstOrDefault(provider => string.Equals(provider.Id, effectiveId, StringComparison.OrdinalIgnoreCase));
    }

    public void EnsureSelectionExists()
    {
        var provider = Providers.FirstOrDefault(item => string.Equals(item.Id, SelectedProviderId, StringComparison.OrdinalIgnoreCase))
            ?? Providers.First();

        var model = provider.Models.FirstOrDefault(item => string.Equals(item.Id, SelectedModelId, StringComparison.OrdinalIgnoreCase))
            ?? provider.Models.FirstOrDefault(item => item.IsDefault)
            ?? provider.Models.First();

        SelectedProviderId = provider.Id;
        SelectedModelId = model.Id;
    }

    public GlobalModelConfiguration BuildGlobalModelConfiguration() =>
        new(
            SelectedProviderId,
            SelectedModelId,
            ApiKeys.TryGetValue(SelectedProviderId, out var apiKey) && !string.IsNullOrWhiteSpace(apiKey),
            ApiKeys.GetValueOrDefault(SelectedProviderId));
}
