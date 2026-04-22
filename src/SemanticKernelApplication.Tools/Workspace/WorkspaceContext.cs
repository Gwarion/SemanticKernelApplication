using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Workspace;

public sealed class WorkspaceContext : IWorkspaceContext
{
    private readonly ILocalWorkbenchConfigurationStore _configurationStore;

    public WorkspaceContext(ILocalWorkbenchConfigurationStore configurationStore) 
        => _configurationStore = configurationStore;

    public string CurrentRootPath 
        => _configurationStore.GetWorkspacePath();

    public string SetRootPath(string workspacePath) 
        => _configurationStore.SetWorkspacePath(workspacePath);
}
