namespace SemanticKernelApplication.Abstractions.Activities;

/// <summary>
/// Represents one item written to the shared activity log.
/// </summary>
public sealed class ActivityLogEntry
{
    private ActivityLogEntry(
        long sequence,
        ActivityKind kind,
        ActivityStatus status,
        ActivitySeverity severity,
        string title,
        string message,
        DateTimeOffset timestampUtc,
        string? sessionId,
        Guid? conversationId,
        Guid? agentId,
        Guid? turnId,
        Guid? correlationId,
        string? delta,
        IReadOnlyDictionary<string, string>? metadata)
    {
        Sequence = sequence;
        Kind = kind;
        Status = status;
        Severity = severity;
        Title = title;
        Message = message;
        TimestampUtc = timestampUtc;
        SessionId = sessionId;
        ConversationId = conversationId;
        AgentId = agentId;
        TurnId = turnId;
        CorrelationId = correlationId;
        Delta = delta;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets a new builder used to create an <see cref="ActivityLogEntry"/>.
    /// </summary>
    public static ActivityLogEntryBuilder Builder => new();

    /// <summary>
    /// Gets the monotonic sequence number assigned to the entry.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// Gets the category of activity being captured.
    /// </summary>
    public ActivityKind Kind { get; }

    /// <summary>
    /// Gets the execution status associated with the entry.
    /// </summary>
    public ActivityStatus Status { get; }

    /// <summary>
    /// Gets the importance of the entry for operators and the UI.
    /// </summary>
    public ActivitySeverity Severity { get; }

    /// <summary>
    /// Gets the short label shown for the entry.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the human-readable description of the event.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the timestamp when the event was recorded.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>
    /// Gets the optional session identifier for correlation.
    /// </summary>
    public string? SessionId { get; }

    /// <summary>
    /// Gets the optional conversation identifier for correlation.
    /// </summary>
    public Guid? ConversationId { get; }

    /// <summary>
    /// Gets the optional agent identifier for correlation.
    /// </summary>
    public Guid? AgentId { get; }

    /// <summary>
    /// Gets the optional turn identifier for correlation.
    /// </summary>
    public Guid? TurnId { get; }

    /// <summary>
    /// Gets the optional operation identifier shared across related events.
    /// </summary>
    public Guid? CorrelationId { get; }

    /// <summary>
    /// Gets the optional incremental text payload for streaming scenarios.
    /// </summary>
    public string? Delta { get; }

    /// <summary>
    /// Gets the optional key-value payload with extra event data.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    /// <summary>
    /// Creates a builder pre-populated with this instance's values.
    /// </summary>
    public ActivityLogEntryBuilder ToBuilder() =>
        Builder
            .WithSequence(Sequence)
            .WithKind(Kind)
            .WithStatus(Status)
            .WithSeverity(Severity)
            .WithTitle(Title)
            .WithMessage(Message)
            .WithTimestampUtc(TimestampUtc)
            .WithSessionId(SessionId)
            .WithConversationId(ConversationId)
            .WithAgentId(AgentId)
            .WithTurnId(TurnId)
            .WithCorrelationId(CorrelationId)
            .WithDelta(Delta)
            .WithMetadata(Metadata);

    /// <summary>
    /// Builds <see cref="ActivityLogEntry"/> instances while keeping call sites readable.
    /// </summary>
    public sealed class ActivityLogEntryBuilder
    {
        private long _sequence;
        private ActivityKind? _kind;
        private ActivityStatus? _status;
        private ActivitySeverity? _severity;
        private string? _title;
        private string? _message;
        private DateTimeOffset _timestampUtc;
        private string? _sessionId;
        private Guid? _conversationId;
        private Guid? _agentId;
        private Guid? _turnId;
        private Guid? _correlationId;
        private string? _delta;
        private IReadOnlyDictionary<string, string>? _metadata;

        /// <summary>
        /// Sets the sequence number assigned to the activity entry.
        /// </summary>
        public ActivityLogEntryBuilder WithSequence(long sequence)
        {
            _sequence = sequence;
            return this;
        }

        /// <summary>
        /// Sets the activity category.
        /// </summary>
        public ActivityLogEntryBuilder WithKind(ActivityKind kind)
        {
            _kind = kind;
            return this;
        }

        /// <summary>
        /// Sets the activity status.
        /// </summary>
        public ActivityLogEntryBuilder WithStatus(ActivityStatus status)
        {
            _status = status;
            return this;
        }

        /// <summary>
        /// Sets the activity severity.
        /// </summary>
        public ActivityLogEntryBuilder WithSeverity(ActivitySeverity severity)
        {
            _severity = severity;
            return this;
        }

        /// <summary>
        /// Sets the short title shown for the entry.
        /// </summary>
        public ActivityLogEntryBuilder WithTitle(string title)
        {
            _title = title;
            return this;
        }

        /// <summary>
        /// Sets the human-readable message shown for the entry.
        /// </summary>
        public ActivityLogEntryBuilder WithMessage(string message)
        {
            _message = message;
            return this;
        }

        /// <summary>
        /// Sets the timestamp when the entry was recorded.
        /// </summary>
        public ActivityLogEntryBuilder WithTimestampUtc(DateTimeOffset timestampUtc)
        {
            _timestampUtc = timestampUtc;
            return this;
        }

        /// <summary>
        /// Sets the optional session identifier.
        /// </summary>
        public ActivityLogEntryBuilder WithSessionId(string? sessionId)
        {
            _sessionId = sessionId;
            return this;
        }

        /// <summary>
        /// Sets the optional conversation identifier.
        /// </summary>
        public ActivityLogEntryBuilder WithConversationId(Guid? conversationId)
        {
            _conversationId = conversationId;
            return this;
        }

        /// <summary>
        /// Sets the optional agent identifier.
        /// </summary>
        public ActivityLogEntryBuilder WithAgentId(Guid? agentId)
        {
            _agentId = agentId;
            return this;
        }

        /// <summary>
        /// Sets the optional turn identifier.
        /// </summary>
        public ActivityLogEntryBuilder WithTurnId(Guid? turnId)
        {
            _turnId = turnId;
            return this;
        }

        /// <summary>
        /// Sets the optional correlation identifier.
        /// </summary>
        public ActivityLogEntryBuilder WithCorrelationId(Guid? correlationId)
        {
            _correlationId = correlationId;
            return this;
        }

        /// <summary>
        /// Sets the optional streaming delta.
        /// </summary>
        public ActivityLogEntryBuilder WithDelta(string? delta)
        {
            _delta = delta;
            return this;
        }

        /// <summary>
        /// Sets the optional metadata dictionary.
        /// </summary>
        public ActivityLogEntryBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata)
        {
            _metadata = metadata;
            return this;
        }

        /// <summary>
        /// Validates the configured values and creates the final entry.
        /// </summary>
        public ActivityLogEntry Build()
        {
            if (_kind is null)
                throw new InvalidOperationException("Activity kind is required.");

            if (_status is null)
                throw new InvalidOperationException("Activity status is required.");

            if (_severity is null)
                throw new InvalidOperationException("Activity severity is required.");

            ArgumentException.ThrowIfNullOrWhiteSpace(_title);
            ArgumentException.ThrowIfNullOrWhiteSpace(_message);

            if (_timestampUtc == default)
                throw new InvalidOperationException("Activity timestamp is required.");

            return new ActivityLogEntry(
                _sequence,
                _kind.Value,
                _status.Value,
                _severity.Value,
                _title,
                _message,
                _timestampUtc,
                _sessionId,
                _conversationId,
                _agentId,
                _turnId,
                _correlationId,
                _delta,
                _metadata);
        }
    }
}
