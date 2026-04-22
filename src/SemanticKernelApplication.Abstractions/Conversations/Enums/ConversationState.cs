namespace SemanticKernelApplication.Abstractions.Conversations;

/// <summary>
/// Represents the lifecycle state of a conversation thread.
/// </summary>
public enum ConversationState
{
    Draft,
    Active,
    Completed,
    Cancelled
}
