namespace SemanticKernelApplication.Runtime.Services.Workbench;

public sealed class ConversationSessionAccessor : IConversationSessionAccessor
{
    public string? ActiveConversationId { get; set; }
}
