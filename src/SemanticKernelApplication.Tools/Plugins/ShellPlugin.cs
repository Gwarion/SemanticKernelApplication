using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SemanticKernelApplication.Tools.Configuration;
using SemanticKernelApplication.Tools.Workspace;

namespace SemanticKernelApplication.Tools.Plugins;

public sealed class ShellPlugin
{
    private readonly WorkspaceToolOptions _options;
    private readonly IWorkspaceContext _workspaceContext;

    public ShellPlugin(IOptions<WorkspaceToolOptions> options, IWorkspaceContext workspaceContext)
    {
        _options = options.Value;
        _workspaceContext = workspaceContext;
    }

    [KernelFunction]
    [Description("Run a PowerShell command from the configured workspace root.")]
    public Task<string> RunPowerShellAsync(string command, CancellationToken cancellationToken = default)
        => RunProcessAsync("powershell", $"-NoLogo -NoProfile -Command \"{command.Replace("\"", "\\\"")}\"", cancellationToken);

    [KernelFunction]
    [Description("Run a bash command from the configured workspace root.")]
    public Task<string> RunBashAsync(string command, CancellationToken cancellationToken = default)
        => RunProcessAsync("bash", $"-lc \"{command.Replace("\"", "\\\"")}\"", cancellationToken);

    private async Task<string> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = _workspaceContext.CurrentRootPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _options.CommandTimeoutSeconds)));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        await process.WaitForExitAsync(linkedCts.Token);

        var text = output.ToString().Trim();
        return string.IsNullOrWhiteSpace(text)
            ? $"Command exited with code {process.ExitCode}."
            : text;
    }
}
