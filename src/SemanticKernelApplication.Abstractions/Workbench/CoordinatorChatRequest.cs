namespace SemanticKernelApplication.Abstractions.Workbench;

/// <summary>
/// Represents one message sent to the coordinator chat.
/// </summary>
/// <param name="Message">User message content.</param>
/// <param name="ConversationId">Optional active conversation identifier.</param>
public sealed record CoordinatorChatRequest(
    string Message,
    string? ConversationId = null);
