using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

public interface IAiProviderCatalog
{
    IReadOnlyList<ModelProviderDefinition> GetProviders();

    ModelProviderDefinition? GetProvider(string? providerId);

    AgentProviderRegistration? GetRegistration(string? providerId);
}
