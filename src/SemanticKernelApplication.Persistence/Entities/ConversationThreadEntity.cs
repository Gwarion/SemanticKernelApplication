namespace SemanticKernelApplication.Persistence.Entities;

internal sealed class ConversationThreadEntity
{
    public string ThreadId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}
