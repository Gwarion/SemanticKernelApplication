using Microsoft.Extensions.Options;
using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Coordinates local workbench configuration state and delegates persistence to the repository layer.
/// </summary>
public sealed class LocalWorkbenchConfigurationStore : ILocalWorkbenchConfigurationStore
{
    private readonly Lock _lock = new();
    private readonly LocalWorkbenchConfigurationRepository _repository;
    private readonly LocalWorkbenchConfigurationState _state;

    public LocalWorkbenchConfigurationStore(
        IOptions<LocalWorkbenchStoreOptions> storeOptions,
        IOptions<WorkspaceToolOptions> workspaceOptions)
    {
        var databasePath = ResolveDatabasePath(storeOptions.Value.DatabasePath);
        var defaultWorkspacePath = NormalizeWorkspace(workspaceOptions.Value.RootPath);
        _repository = new LocalWorkbenchConfigurationRepository(databasePath);
        _state = _repository.LoadState(defaultWorkspacePath, LocalWorkbenchSeedCatalog.Providers);
    }

    public IReadOnlyList<AgentProviderRegistration> GetProviders()
    {
        lock (_lock)
            return _state.Providers.ToArray();
    }

    public AgentProviderRegistration? GetProvider(string? providerId)
    {
        lock (_lock)
            return _state.GetProvider(providerId);
    }

    public IReadOnlyList<string> GetKnownWorkspacePaths()
    {
        lock (_lock)
            return _state.KnownWorkspacePaths.ToArray();
    }

    public GlobalModelConfiguration GetGlobalModelConfiguration()
    {
        lock (_lock)
            return _state.BuildGlobalModelConfiguration();
    }

    public string? GetApiKey(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return null;

        lock (_lock)
            return _state.ApiKeys.GetValueOrDefault(providerId.Trim());
    }

    public string GetWorkspacePath()
    {
        lock (_lock)
            return _state.WorkspacePath;
    }

    public string SetWorkspacePath(string workspacePath)
    {
        var normalizedWorkspace = NormalizeWorkspace(workspacePath);

        lock (_lock)
        {
            _repository.PersistWorkspace(_state, normalizedWorkspace);
            return _state.WorkspacePath;
        }
    }

    public GlobalModelConfiguration UpdateGlobalModelConfiguration(GlobalModelConfigurationRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SelectedProviderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SelectedModelId);

        lock (_lock)
        {
            var provider = _state.Providers.FirstOrDefault(item => string.Equals(item.Id, request.SelectedProviderId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Provider '{request.SelectedProviderId}' was not found.");
            var model = provider.Models.FirstOrDefault(item => string.Equals(item.Id, request.SelectedModelId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Model '{request.SelectedModelId}' was not found for provider '{provider.DisplayName}'.");

            _repository.PersistGlobalConfiguration(_state, provider.Id, model.Id, request.ApiKey);
            return _state.BuildGlobalModelConfiguration();
        }
    }

    internal static string ResolveDatabasePath(string databasePath)
    {
        var candidate = string.IsNullOrWhiteSpace(databasePath)
            ? ".appdata\\workbench.db"
            : databasePath.Trim();
        return Path.GetFullPath(candidate);
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
