using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

public sealed class ConfigurationAiProviderCatalog : IAiProviderCatalog
{
    private readonly ILocalWorkbenchConfigurationStore _configurationStore;
    private readonly IProviderSessionConfiguration _sessionConfiguration;

    public ConfigurationAiProviderCatalog(
        ILocalWorkbenchConfigurationStore configurationStore,
        IProviderSessionConfiguration sessionConfiguration)
    {
        _configurationStore = configurationStore;
        _sessionConfiguration = sessionConfiguration;
    }

    public ModelProviderDefinition? GetProvider(string? providerId)
    {
        var provider = GetRegistration(providerId);
        return provider is null ? null : ToDefinition(provider);
    }

    public IReadOnlyList<ModelProviderDefinition> GetProviders() =>
        _configurationStore.GetProviders().Select(ToDefinition).ToArray();

    public AgentProviderRegistration? GetRegistration(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return _sessionConfiguration.ResolveSelectedProvider();
        }

        return _configurationStore.GetProvider(providerId);
    }

    private ModelProviderDefinition ToDefinition(AgentProviderRegistration provider)
    {
        var configuration = _sessionConfiguration.GetConfiguration();
        var isSelected = string.Equals(configuration.SelectedProviderId, provider.Id, StringComparison.OrdinalIgnoreCase);
        var selectedModelId = isSelected
            ? configuration.SelectedModelId
            : provider.Models.FirstOrDefault(model => model.IsDefault)?.Id ?? provider.Models.First().Id;

        var models = provider.Models
            .Select(model => new ModelDefinition(model.Id, model.DisplayName, model.IsDefault))
            .ToArray();

        return new ModelProviderDefinition(
            provider.Id,
            provider.DisplayName,
            provider.Kind,
            models,
            selectedModelId,
            isSelected && configuration.ApiKeyConfigured,
            provider.IsDefault);
    }
}
