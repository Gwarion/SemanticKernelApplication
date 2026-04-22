namespace SemanticKernelApplication.Persistence.Entities;

internal sealed class KnownWorkspaceEntity
{
    public string WorkspacePath { get; set; } = string.Empty;
    public DateTimeOffset LastUsedUtc { get; set; }
}
