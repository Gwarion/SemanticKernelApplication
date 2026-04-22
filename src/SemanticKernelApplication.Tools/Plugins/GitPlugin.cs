using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SemanticKernelApplication.Tools.Plugins;

public sealed class GitPlugin
{
    private readonly ShellPlugin _shellPlugin;

    public GitPlugin(ShellPlugin shellPlugin) 
        => _shellPlugin = shellPlugin;

    [KernelFunction]
    [Description("Show the current git status for the workspace.")]
    public Task<string> StatusAsync(CancellationToken cancellationToken = default)
        => _shellPlugin.RunPowerShellAsync("git status --short --branch", cancellationToken);

    [KernelFunction]
    [Description("Show a compact git diff summary.")]
    public Task<string> DiffSummaryAsync(CancellationToken cancellationToken = default)
        => _shellPlugin.RunPowerShellAsync("git diff --stat", cancellationToken);

    [KernelFunction]
    [Description("List the most recent commits.")]
    public Task<string> RecentCommitsAsync(int count = 5, CancellationToken cancellationToken = default)
        => _shellPlugin.RunPowerShellAsync($"git log --oneline -n {Math.Clamp(count, 1, 20)}", cancellationToken);
}
