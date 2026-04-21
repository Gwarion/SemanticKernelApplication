using Microsoft.Extensions.Options;
using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

public sealed class ConfigurationAiProviderCatalog : IAiProviderCatalog
{
    private readonly IReadOnlyList<ModelProviderDefinition> _providers;

    public ConfigurationAiProviderCatalog(IOptions<AgentProviderOptions> options)
    {
        _providers = options.Value.Providers
            .Select(provider => new ModelProviderDefinition(
                provider.Id,
                provider.DisplayName,
                provider.Kind,
                provider.ModelId,
                IsConfigured(provider),
                provider.IsDefault))
            .ToArray();
    }

    public ModelProviderDefinition? GetProvider(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return _providers.FirstOrDefault(provider => provider.IsDefault) ?? _providers.FirstOrDefault();
        }

        return _providers.FirstOrDefault(provider => string.Equals(provider.Id, providerId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ModelProviderDefinition> GetProviders() => _providers;

    private static bool IsConfigured(AgentProviderRegistration provider)
    {
        return provider.Kind == AiProviderKind.Demo || !string.IsNullOrWhiteSpace(provider.ApiKey);
    }
}
