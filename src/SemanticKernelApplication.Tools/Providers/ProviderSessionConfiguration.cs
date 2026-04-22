using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

public sealed class ProviderSessionConfiguration : IProviderSessionConfiguration
{
    private readonly ILocalWorkbenchConfigurationStore _configurationStore;

    public ProviderSessionConfiguration(ILocalWorkbenchConfigurationStore configurationStore)
    {
        _configurationStore = configurationStore;
    }

    public string? SelectedProviderId => _configurationStore.GetGlobalModelConfiguration().SelectedProviderId;

    public string? SelectedModelId => _configurationStore.GetGlobalModelConfiguration().SelectedModelId;

    public string? ApiKey => _configurationStore.GetGlobalModelConfiguration().ApiKey;

    public GlobalModelConfiguration GetConfiguration() => _configurationStore.GetGlobalModelConfiguration();

    public GlobalModelConfiguration Update(GlobalModelConfigurationRequest request) =>
        _configurationStore.UpdateGlobalModelConfiguration(request);

    public AgentProviderRegistration? ResolveSelectedProvider() =>
        _configurationStore.GetProvider(SelectedProviderId);
}
