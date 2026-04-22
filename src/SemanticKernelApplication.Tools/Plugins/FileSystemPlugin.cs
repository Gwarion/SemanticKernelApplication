using System.ComponentModel;
using Microsoft.SemanticKernel;
using SemanticKernelApplication.Tools.Workspace;

namespace SemanticKernelApplication.Tools.Plugins;

public sealed class FileSystemPlugin
{
    private readonly IWorkspaceContext _workspaceContext;

    public FileSystemPlugin(IWorkspaceContext workspaceContext)
        => _workspaceContext = workspaceContext;

    [KernelFunction]
    [Description("Read a UTF-8 text file from the workspace root.")]
    public async Task<string> ReadFileAsync(
        [Description("Relative path under the workspace root.")] string path,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(path);
        return await File.ReadAllTextAsync(fullPath, cancellationToken);
    }

    [KernelFunction]
    [Description("Write UTF-8 text content to a file under the workspace root.")]
    public async Task<string> WriteFileAsync(
        [Description("Relative path under the workspace root.")] string path,
        [Description("Content to write to the file.")] string content,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content, cancellationToken);
        return $"Wrote {path}";
    }

    [KernelFunction]
    [Description("List files and folders under a workspace directory.")]
    public string ListDirectory([Description("Relative path under the workspace root.")] string path = ".")
    {
        var fullPath = ResolvePath(path);

        if (!Directory.Exists(fullPath))
            return $"Directory '{path}' does not exist.";

        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFileSystemEntries(fullPath)
                .Select(entry => Path.GetRelativePath(_workspaceContext.CurrentRootPath, entry))
                .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase));
    }

    private string ResolvePath(string path)
    {
        var root = _workspaceContext.CurrentRootPath;
        var combined = Path.GetFullPath(Path.Combine(root, string.IsNullOrWhiteSpace(path) ? "." : path));

        if (!IsWithinRoot(root, combined))
            throw new InvalidOperationException("Path escapes the configured workspace root.");

        return combined;
    }

    private static bool IsWithinRoot(string root, string path)
    {
        var normalizedRoot = $"{root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}{Path.DirectorySeparatorChar}";

        return string.Equals(root, path, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }
}
