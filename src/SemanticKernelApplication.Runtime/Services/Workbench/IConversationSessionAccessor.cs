namespace SemanticKernelApplication.Runtime.Services.Workbench;

public interface IConversationSessionAccessor
{
    string? ActiveConversationId { get; set; }
}
