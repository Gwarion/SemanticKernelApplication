namespace SemanticKernelApplication.Runtime.Services.Workbench;

public sealed class ConversationSessionAccessor : IConversationSessionAccessor
{
    public Guid? ActiveConversationId { get; set; }
}
