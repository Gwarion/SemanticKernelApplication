namespace SemanticKernelApplication.Abstractions.Conversations;

/// <summary>
/// Identifies the type of participant taking part in a conversation.
/// </summary>
public enum ConversationParticipantKind
{
    User,
    Agent,
    Coordinator,
    Tool
}
