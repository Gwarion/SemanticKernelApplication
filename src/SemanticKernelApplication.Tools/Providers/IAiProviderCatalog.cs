using SemanticKernelApplication.Abstractions.Providers;

namespace SemanticKernelApplication.Tools.Providers;

public interface IAiProviderCatalog
{
    IReadOnlyList<ModelProviderDefinition> GetProviders();

    ModelProviderDefinition? GetProvider(string? providerId);
}
