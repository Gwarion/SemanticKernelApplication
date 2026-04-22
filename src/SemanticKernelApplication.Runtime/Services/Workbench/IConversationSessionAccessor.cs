namespace SemanticKernelApplication.Runtime.Services.Workbench;

public interface IConversationSessionAccessor
{
    Guid? ActiveConversationId { get; set; }
}
