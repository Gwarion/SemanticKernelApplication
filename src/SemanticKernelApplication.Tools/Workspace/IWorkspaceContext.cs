namespace SemanticKernelApplication.Tools.Workspace;

/// <summary>
/// Tracks the active workspace root used by file and shell tools.
/// </summary>
public interface IWorkspaceContext
{
    /// <summary>
    /// Gets the current workspace root path.
    /// </summary>
    string CurrentRootPath { get; }

    /// <summary>
    /// Updates the active workspace root path.
    /// </summary>
    /// <param name="workspacePath">Workspace path that should become active.</param>
    string SetRootPath(string workspacePath);
}
