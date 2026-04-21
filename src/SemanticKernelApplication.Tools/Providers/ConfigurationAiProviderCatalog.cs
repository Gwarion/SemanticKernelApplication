using Microsoft.Extensions.Options;
using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

public sealed class ConfigurationAiProviderCatalog : IAiProviderCatalog
{
    private readonly AgentProviderOptions _options;
    private readonly IProviderSessionConfiguration _sessionConfiguration;

    public ConfigurationAiProviderCatalog(
        IOptions<AgentProviderOptions> options,
        IProviderSessionConfiguration sessionConfiguration)
    {
        _options = options.Value;
        _sessionConfiguration = sessionConfiguration;
    }

    public ModelProviderDefinition? GetProvider(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            var selected = _sessionConfiguration.ResolveSelectedProvider(_options.Providers);
            return selected is null ? null : ToDefinition(selected);
        }

        var provider = _options.Providers.FirstOrDefault(provider => string.Equals(provider.Id, providerId, StringComparison.OrdinalIgnoreCase));
        return provider is null ? null : ToDefinition(provider);
    }

    public IReadOnlyList<ModelProviderDefinition> GetProviders() => _options.Providers.Select(ToDefinition).ToArray();

    private ModelProviderDefinition ToDefinition(AgentProviderRegistration provider)
    {
        var selected = _sessionConfiguration.ResolveSelectedProvider(_options.Providers);
        var apiKeyConfigured = provider.Kind == AiProviderKind.Demo
            || (selected is not null
                && string.Equals(selected.Id, provider.Id, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(_sessionConfiguration.ApiKey));

        var isSelected = selected is not null && string.Equals(selected.Id, provider.Id, StringComparison.OrdinalIgnoreCase);
        var isDefault = selected is null
            ? provider.IsDefault
            : isSelected;

        return new ModelProviderDefinition(
            provider.Id,
            provider.DisplayName,
            provider.Kind,
            provider.ModelId,
            apiKeyConfigured,
            isDefault);
    }
}
