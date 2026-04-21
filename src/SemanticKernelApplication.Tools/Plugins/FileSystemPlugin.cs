using System.ComponentModel;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Tools.Plugins;

public sealed class FileSystemPlugin
{
    private readonly WorkspaceToolOptions _options;

    public FileSystemPlugin(IOptions<WorkspaceToolOptions> options)
    {
        _options = options.Value;
    }

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
        {
            return $"Directory '{path}' does not exist.";
        }

        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFileSystemEntries(fullPath)
                .Select(entry => Path.GetRelativePath(_options.RootPath, entry))
                .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase));
    }

    private string ResolvePath(string path)
    {
        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(_options.RootPath) ? Environment.CurrentDirectory : _options.RootPath);
        var combined = Path.GetFullPath(Path.Combine(root, string.IsNullOrWhiteSpace(path) ? "." : path));
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path escapes the configured workspace root.");
        }

        return combined;
    }
}
