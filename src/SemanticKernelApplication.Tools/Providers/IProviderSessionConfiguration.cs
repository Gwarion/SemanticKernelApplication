using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

public interface IProviderSessionConfiguration
{
    string? SelectedProviderId { get; }

    string? SelectedModelId { get; }

    string? ApiKey { get; }

    GlobalModelConfiguration GetConfiguration();

    GlobalModelConfiguration Update(GlobalModelConfigurationRequest request);

    AgentProviderRegistration? ResolveSelectedProvider();
}
