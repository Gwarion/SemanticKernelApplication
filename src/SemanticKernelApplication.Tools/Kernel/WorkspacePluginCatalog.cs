using SemanticKernelApplication.Tools.Plugins;

namespace SemanticKernelApplication.Tools.Kernel;

public sealed class WorkspacePluginCatalog : IWorkspacePluginCatalog
{
    private readonly FileSystemPlugin _fileSystemPlugin;
    private readonly GitPlugin _gitPlugin;
    private readonly ShellPlugin _shellPlugin;

    public WorkspacePluginCatalog(
        FileSystemPlugin fileSystemPlugin,
        GitPlugin gitPlugin,
        ShellPlugin shellPlugin)
    {
        _fileSystemPlugin = fileSystemPlugin;
        _gitPlugin = gitPlugin;
        _shellPlugin = shellPlugin;
    }

    public IEnumerable<WorkspacePluginRegistration> GetPlugins()
    {
        yield return new WorkspacePluginRegistration("filesystem", _fileSystemPlugin);
        yield return new WorkspacePluginRegistration("git", _gitPlugin);
        yield return new WorkspacePluginRegistration("shell", _shellPlugin);
    }
}
