namespace SemanticKernelApplication.Abstractions.Conversations;

public enum ConversationParticipantKind
{
    User,
    Agent,
    Coordinator,
    Tool
}

public enum ConversationMessageRole
{
    System,
    User,
    Assistant,
    Tool
}

public enum ConversationState
{
    Draft,
    Active,
    Completed,
    Cancelled
}

public sealed record ConversationParticipant(
    string ParticipantId,
    string DisplayName,
    ConversationParticipantKind Kind,
    string? AgentId = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ConversationMessage(
    string MessageId,
    string ThreadId,
    ConversationMessageRole Role,
    string AuthorId,
    string Content,
    DateTimeOffset CreatedAtUtc,
    string? TurnId = null,
    string? ParentMessageId = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ConversationTurn(
    string TurnId,
    string ThreadId,
    int Sequence,
    string InitiatedByParticipantId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc = null,
    string? Goal = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ConversationThread(
    string ThreadId,
    string Title,
    ConversationState State,
    IReadOnlyList<ConversationParticipant> Participants,
    IReadOnlyList<ConversationTurn> Turns,
    IReadOnlyList<ConversationMessage> Messages,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public interface IConversationStore
{
    Task<ConversationThread?> GetAsync(string threadId, CancellationToken cancellationToken = default);

    Task<ConversationThread> SaveAsync(ConversationThread thread, CancellationToken cancellationToken = default);
}
