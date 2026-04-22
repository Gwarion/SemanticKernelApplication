namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Configures tool access to the local workspace.
/// </summary>
public sealed class WorkspaceToolOptions
{
    /// <summary>
    /// Gets or sets the default workspace root path.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum shell command duration in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
