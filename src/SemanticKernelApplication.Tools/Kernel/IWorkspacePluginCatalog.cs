namespace SemanticKernelApplication.Tools.Kernel;

public interface IWorkspacePluginCatalog
{
    IEnumerable<WorkspacePluginRegistration> GetPlugins();
}

public sealed record WorkspacePluginRegistration(string Name, object Instance);
