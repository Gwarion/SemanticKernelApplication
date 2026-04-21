namespace SemanticKernelApplication.Tools.Configuration;

public sealed class WorkspaceToolOptions
{
    public string RootPath { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 30;
}
