using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Providers;

public sealed class ProviderSessionConfiguration : IProviderSessionConfiguration
{
    private readonly Lock _lock = new();
    private string? _selectedProviderId;
    private string? _apiKey;

    public string? SelectedProviderId
    {
        get
        {
            lock (_lock)
            {
                return _selectedProviderId;
            }
        }
    }

    public string? ApiKey
    {
        get
        {
            lock (_lock)
            {
                return _apiKey;
            }
        }
    }

    public GlobalModelConfiguration GetConfiguration(IReadOnlyList<AgentProviderRegistration> providers)
    {
        var provider = ResolveSelectedProvider(providers)
            ?? throw new InvalidOperationException("No providers are configured.");

        var apiKeyConfigured = provider.Kind == Abstractions.Providers.AiProviderKind.Demo || !string.IsNullOrWhiteSpace(ApiKey);
        return new GlobalModelConfiguration(provider.Id, apiKeyConfigured);
    }

    public AgentProviderRegistration? ResolveSelectedProvider(IReadOnlyList<AgentProviderRegistration> providers)
    {
        lock (_lock)
        {
            if (!string.IsNullOrWhiteSpace(_selectedProviderId))
            {
                return providers.FirstOrDefault(provider => string.Equals(provider.Id, _selectedProviderId, StringComparison.OrdinalIgnoreCase));
            }

            return providers.FirstOrDefault(provider => provider.IsDefault)
                ?? providers.FirstOrDefault();
        }
    }

    public GlobalModelConfiguration Update(GlobalModelConfigurationRequest request, IReadOnlyList<AgentProviderRegistration> providers)
    {
        if (string.IsNullOrWhiteSpace(request.SelectedProviderId))
        {
            throw new ArgumentException("A global provider selection is required.", nameof(request));
        }

        var provider = providers.FirstOrDefault(item => string.Equals(item.Id, request.SelectedProviderId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Provider '{request.SelectedProviderId}' was not found.");

        lock (_lock)
        {
            _selectedProviderId = provider.Id;
            _apiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? null : request.ApiKey.Trim();

            return new GlobalModelConfiguration(
                provider.Id,
                provider.Kind == Abstractions.Providers.AiProviderKind.Demo || !string.IsNullOrWhiteSpace(_apiKey));
        }
    }
}
