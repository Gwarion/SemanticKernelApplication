namespace SemanticKernelApplication.Abstractions.Workbench;

/// <summary>
/// Requests that the workbench switch to a specific workspace path.
/// </summary>
/// <param name="WorkspacePath">Path that should become the active workspace.</param>
public sealed record WorkspaceSelectionRequest(string WorkspacePath);
