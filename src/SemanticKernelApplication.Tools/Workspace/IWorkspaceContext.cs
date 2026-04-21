namespace SemanticKernelApplication.Tools.Workspace;

public interface IWorkspaceContext
{
    string CurrentRootPath { get; }

    string SetRootPath(string workspacePath);
}
