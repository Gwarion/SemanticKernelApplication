using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Coordinates local workbench configuration state and delegates persistence to the repository layer.
/// </summary>
public sealed class LocalWorkbenchConfigurationStore : ILocalWorkbenchConfigurationStore
{
    private readonly Lock _lock = new();
    private readonly ILocalWorkbenchConfigurationRepository _repository;
    private LocalWorkbenchConfigurationRecord _configuration;

    public LocalWorkbenchConfigurationStore(
        ILocalWorkbenchConfigurationRepository repository,
        WorkspaceToolOptions workspaceOptions)
    {
        _repository = repository;
        _configuration = _repository.Load(NormalizeWorkspace(workspaceOptions.RootPath));
    }

    public IReadOnlyList<AgentProviderRegistration> GetProviders()
    {
        lock (_lock)
            return _configuration.Providers.ToArray();
    }

    public AgentProviderRegistration? GetProvider(string? providerId)
    {
        lock (_lock)
            return _configuration.GetProvider(providerId);
    }

    public IReadOnlyList<string> GetKnownWorkspacePaths()
    {
        lock (_lock)
            return _configuration.KnownWorkspacePaths.ToArray();
    }

    public GlobalModelConfiguration GetGlobalModelConfiguration()
    {
        lock (_lock)
            return _configuration.ToGlobalModelConfiguration();
    }

    public string? GetApiKey(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return null;

        lock (_lock)
            return _configuration.ApiKeys.GetValueOrDefault(providerId.Trim());
    }

    public string GetWorkspacePath()
    {
        lock (_lock)
            return _configuration.WorkspacePath;
    }

    public string SetWorkspacePath(string workspacePath)
    {
        var normalizedWorkspace = NormalizeWorkspace(workspacePath);

        lock (_lock)
        {
            _configuration = _repository.SaveWorkspace(_configuration, normalizedWorkspace);
            return _configuration.WorkspacePath;
        }
    }

    public GlobalModelConfiguration UpdateGlobalModelConfiguration(GlobalModelConfigurationRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SelectedProviderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SelectedModelId);

        lock (_lock)
        {
            var provider = _configuration.Providers.FirstOrDefault(item => string.Equals(item.Id, request.SelectedProviderId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Provider '{request.SelectedProviderId}' was not found.");
            var model = provider.Models.FirstOrDefault(item => string.Equals(item.Id, request.SelectedModelId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Model '{request.SelectedModelId}' was not found for provider '{provider.DisplayName}'.");

            _configuration = _repository.SaveGlobalModelConfiguration(_configuration, provider.Id, model.Id, request.ApiKey);
            return _configuration.ToGlobalModelConfiguration();
        }
    }

    internal static string NormalizeWorkspace(string workspacePath)
    {
        var candidate = string.IsNullOrWhiteSpace(workspacePath)
            ? Environment.CurrentDirectory
            : workspacePath.Trim();
        var fullPath = Path.GetFullPath(candidate);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"Workspace folder '{fullPath}' does not exist.");

        return fullPath;
    }
}
