using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

public interface IProviderSessionConfiguration
{
    string? SelectedProviderId { get; }

    string? ApiKey { get; }

    GlobalModelConfiguration GetConfiguration(IReadOnlyList<AgentProviderRegistration> providers);

    GlobalModelConfiguration Update(GlobalModelConfigurationRequest request, IReadOnlyList<AgentProviderRegistration> providers);

    AgentProviderRegistration? ResolveSelectedProvider(IReadOnlyList<AgentProviderRegistration> providers);
}
