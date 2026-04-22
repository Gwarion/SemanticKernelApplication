using System.Text.Json.Serialization;

namespace SemanticKernelApplication.Abstractions.Conversations;

/// <summary>
/// Groups the messages and metadata that belong to one conversational turn.
/// </summary>
public sealed class ConversationTurn
{
    [JsonConstructor]
    private ConversationTurn(
        Guid turnId,
        Guid threadId,
        int sequence,
        string initiatedByParticipantId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? completedAtUtc,
        string? goal,
        IReadOnlyDictionary<string, string>? metadata)
    {
        TurnId = turnId;
        ThreadId = threadId;
        Sequence = sequence;
        InitiatedByParticipantId = initiatedByParticipantId;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        Goal = goal;
        Metadata = metadata;
    }

    public static ConversationTurnBuilder Builder => new();

    public Guid TurnId { get; }
    public Guid ThreadId { get; }
    public int Sequence { get; }
    public string InitiatedByParticipantId { get; }
    public DateTimeOffset StartedAtUtc { get; }
    public DateTimeOffset? CompletedAtUtc { get; }
    public string? Goal { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public ConversationTurnBuilder ToBuilder() =>
        Builder
            .WithTurnId(TurnId)
            .WithThreadId(ThreadId)
            .WithSequence(Sequence)
            .WithInitiatedByParticipantId(InitiatedByParticipantId)
            .WithStartedAtUtc(StartedAtUtc)
            .WithCompletedAtUtc(CompletedAtUtc)
            .WithGoal(Goal)
            .WithMetadata(Metadata);

    public sealed class ConversationTurnBuilder
    {
        private Guid? _turnId;
        private Guid? _threadId;
        private int? _sequence;
        private string? _initiatedByParticipantId;
        private DateTimeOffset _startedAtUtc;
        private DateTimeOffset? _completedAtUtc;
        private string? _goal;
        private IReadOnlyDictionary<string, string>? _metadata;

        public ConversationTurnBuilder WithTurnId(Guid turnId) { _turnId = turnId; return this; }
        public ConversationTurnBuilder WithThreadId(Guid threadId) { _threadId = threadId; return this; }
        public ConversationTurnBuilder WithSequence(int sequence) { _sequence = sequence; return this; }
        public ConversationTurnBuilder WithInitiatedByParticipantId(string initiatedByParticipantId) { _initiatedByParticipantId = initiatedByParticipantId; return this; }
        public ConversationTurnBuilder WithStartedAtUtc(DateTimeOffset startedAtUtc) { _startedAtUtc = startedAtUtc; return this; }
        public ConversationTurnBuilder WithCompletedAtUtc(DateTimeOffset? completedAtUtc) { _completedAtUtc = completedAtUtc; return this; }
        public ConversationTurnBuilder WithGoal(string? goal) { _goal = goal; return this; }
        public ConversationTurnBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata) { _metadata = metadata; return this; }

        public ConversationTurn Build()
        {
            if (_turnId is null || _turnId == Guid.Empty) throw new InvalidOperationException("Turn id is required.");
            if (_threadId is null || _threadId == Guid.Empty) throw new InvalidOperationException("Thread id is required.");
            if (_sequence is null) throw new InvalidOperationException("Turn sequence is required.");
            if (string.IsNullOrWhiteSpace(_initiatedByParticipantId)) throw new InvalidOperationException("Initiator is required.");
            if (_startedAtUtc == default) throw new InvalidOperationException("Start timestamp is required.");

            return new ConversationTurn(_turnId.Value, _threadId.Value, _sequence.Value, _initiatedByParticipantId, _startedAtUtc, _completedAtUtc, _goal, _metadata);
        }
    }
}
