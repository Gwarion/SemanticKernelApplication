namespace SemanticKernelApplication.Abstractions.Conversations;

/// <summary>
/// Represents the semantic role of a conversation message.
/// </summary>
public enum ConversationMessageRole
{
    System,
    User,
    Assistant,
    Tool
}
