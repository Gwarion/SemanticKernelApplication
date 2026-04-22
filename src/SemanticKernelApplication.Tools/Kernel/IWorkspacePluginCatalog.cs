namespace SemanticKernelApplication.Tools.Kernel;

/// <summary>
/// Provides the set of Semantic Kernel plugins available to the runtime.
/// </summary>
public interface IWorkspacePluginCatalog
{
    /// <summary>
    /// Returns all plugin registrations that should be loaded into a kernel instance.
    /// </summary>
    IEnumerable<WorkspacePluginRegistration> GetPlugins();
}
