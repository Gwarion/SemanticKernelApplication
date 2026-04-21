using Microsoft.Extensions.Options;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Workspace;

public sealed class WorkspaceContext : IWorkspaceContext
{
    private readonly Lock _lock = new();
    private string _currentRootPath;

    public WorkspaceContext(IOptions<WorkspaceToolOptions> options)
    {
        _currentRootPath = NormalizeAndValidate(options.Value.RootPath);
    }

    public string CurrentRootPath
    {
        get
        {
            lock (_lock)
            {
                return _currentRootPath;
            }
        }
    }

    public string SetRootPath(string workspacePath)
    {
        var normalized = NormalizeAndValidate(workspacePath);

        lock (_lock)
        {
            _currentRootPath = normalized;
            return _currentRootPath;
        }
    }

    private static string NormalizeAndValidate(string workspacePath)
    {
        var candidate = string.IsNullOrWhiteSpace(workspacePath)
            ? Environment.CurrentDirectory
            : workspacePath.Trim();

        var fullPath = Path.GetFullPath(candidate);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Workspace folder '{fullPath}' does not exist.");
        }

        return fullPath;
    }
}
