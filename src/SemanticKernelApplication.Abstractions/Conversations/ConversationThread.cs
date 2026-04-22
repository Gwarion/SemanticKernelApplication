using System.Text.Json.Serialization;

namespace SemanticKernelApplication.Abstractions.Conversations;

/// <summary>
/// Represents a complete conversation thread with participants, turns, and messages.
/// </summary>
public sealed class ConversationThread
{
    [JsonConstructor]
    private ConversationThread(
        string threadId,
        string title,
        ConversationState state,
        IReadOnlyList<ConversationParticipant> participants,
        IReadOnlyList<ConversationTurn> turns,
        IReadOnlyList<ConversationMessage> messages,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? updatedAtUtc,
        IReadOnlyDictionary<string, string>? metadata)
    {
        ThreadId = threadId;
        Title = title;
        State = state;
        Participants = participants;
        Turns = turns;
        Messages = messages;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Metadata = metadata;
    }

    public static ConversationThreadBuilder Builder => new();

    public string ThreadId { get; }
    public string Title { get; }
    public ConversationState State { get; }
    public IReadOnlyList<ConversationParticipant> Participants { get; }
    public IReadOnlyList<ConversationTurn> Turns { get; }
    public IReadOnlyList<ConversationMessage> Messages { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? UpdatedAtUtc { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public ConversationThreadBuilder ToBuilder() =>
        Builder
            .WithThreadId(ThreadId)
            .WithTitle(Title)
            .WithState(State)
            .WithParticipants(Participants)
            .WithTurns(Turns)
            .WithMessages(Messages)
            .WithCreatedAtUtc(CreatedAtUtc)
            .WithUpdatedAtUtc(UpdatedAtUtc)
            .WithMetadata(Metadata);

    public sealed class ConversationThreadBuilder
    {
        private string? _threadId;
        private string? _title;
        private ConversationState? _state;
        private IReadOnlyList<ConversationParticipant>? _participants;
        private IReadOnlyList<ConversationTurn>? _turns;
        private IReadOnlyList<ConversationMessage>? _messages;
        private DateTimeOffset _createdAtUtc;
        private DateTimeOffset? _updatedAtUtc;
        private IReadOnlyDictionary<string, string>? _metadata;

        public ConversationThreadBuilder WithThreadId(string threadId) { _threadId = threadId; return this; }
        public ConversationThreadBuilder WithTitle(string title) { _title = title; return this; }
        public ConversationThreadBuilder WithState(ConversationState state) { _state = state; return this; }
        public ConversationThreadBuilder WithParticipants(IReadOnlyList<ConversationParticipant> participants) { _participants = participants; return this; }
        public ConversationThreadBuilder WithTurns(IReadOnlyList<ConversationTurn> turns) { _turns = turns; return this; }
        public ConversationThreadBuilder WithMessages(IReadOnlyList<ConversationMessage> messages) { _messages = messages; return this; }
        public ConversationThreadBuilder WithCreatedAtUtc(DateTimeOffset createdAtUtc) { _createdAtUtc = createdAtUtc; return this; }
        public ConversationThreadBuilder WithUpdatedAtUtc(DateTimeOffset? updatedAtUtc) { _updatedAtUtc = updatedAtUtc; return this; }
        public ConversationThreadBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata) { _metadata = metadata; return this; }

        public ConversationThread Build()
        {
            if (string.IsNullOrWhiteSpace(_threadId)) throw new InvalidOperationException("Thread id is required.");
            if (string.IsNullOrWhiteSpace(_title)) throw new InvalidOperationException("Thread title is required.");
            if (_state is null) throw new InvalidOperationException("Thread state is required.");
            if (_participants is null) throw new InvalidOperationException("Participants are required.");
            if (_turns is null) throw new InvalidOperationException("Turns are required.");
            if (_messages is null) throw new InvalidOperationException("Messages are required.");
            if (_createdAtUtc == default) throw new InvalidOperationException("Created timestamp is required.");

            return new ConversationThread(_threadId, _title, _state.Value, _participants, _turns, _messages, _createdAtUtc, _updatedAtUtc, _metadata);
        }
    }
}
