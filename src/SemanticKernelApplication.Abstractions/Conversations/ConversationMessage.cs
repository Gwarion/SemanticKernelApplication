using System.Text.Json.Serialization;

namespace SemanticKernelApplication.Abstractions.Conversations;

/// <summary>
/// Represents one message inside a conversation thread.
/// </summary>
public sealed class ConversationMessage
{
    [JsonConstructor]
    private ConversationMessage(
        Guid messageId,
        Guid threadId,
        ConversationMessageRole role,
        string authorId,
        string content,
        DateTimeOffset createdAtUtc,
        Guid? turnId,
        Guid? parentMessageId,
        IReadOnlyDictionary<string, string>? metadata)
    {
        MessageId = messageId;
        ThreadId = threadId;
        Role = role;
        AuthorId = authorId;
        Content = content;
        CreatedAtUtc = createdAtUtc;
        TurnId = turnId;
        ParentMessageId = parentMessageId;
        Metadata = metadata;
    }

    public static ConversationMessageBuilder Builder => new();

    public Guid MessageId { get; }
    public Guid ThreadId { get; }
    public ConversationMessageRole Role { get; }
    public string AuthorId { get; }
    public string Content { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid? TurnId { get; }
    public Guid? ParentMessageId { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public ConversationMessageBuilder ToBuilder() =>
        Builder
            .WithMessageId(MessageId)
            .WithThreadId(ThreadId)
            .WithRole(Role)
            .WithAuthorId(AuthorId)
            .WithContent(Content)
            .WithCreatedAtUtc(CreatedAtUtc)
            .WithTurnId(TurnId)
            .WithParentMessageId(ParentMessageId)
            .WithMetadata(Metadata);

    public sealed class ConversationMessageBuilder
    {
        private Guid? _messageId;
        private Guid? _threadId;
        private ConversationMessageRole? _role;
        private string? _authorId;
        private string? _content;
        private DateTimeOffset _createdAtUtc;
        private Guid? _turnId;
        private Guid? _parentMessageId;
        private IReadOnlyDictionary<string, string>? _metadata;

        public ConversationMessageBuilder WithMessageId(Guid messageId) { _messageId = messageId; return this; }
        public ConversationMessageBuilder WithThreadId(Guid threadId) { _threadId = threadId; return this; }
        public ConversationMessageBuilder WithRole(ConversationMessageRole role) { _role = role; return this; }
        public ConversationMessageBuilder WithAuthorId(string authorId) { _authorId = authorId; return this; }
        public ConversationMessageBuilder WithContent(string content) { _content = content; return this; }
        public ConversationMessageBuilder WithCreatedAtUtc(DateTimeOffset createdAtUtc) { _createdAtUtc = createdAtUtc; return this; }
        public ConversationMessageBuilder WithTurnId(Guid? turnId) { _turnId = turnId; return this; }
        public ConversationMessageBuilder WithParentMessageId(Guid? parentMessageId) { _parentMessageId = parentMessageId; return this; }
        public ConversationMessageBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata) { _metadata = metadata; return this; }

        public ConversationMessage Build()
        {
            if (_messageId is null || _messageId == Guid.Empty) throw new InvalidOperationException("Message id is required.");
            if (_threadId is null || _threadId == Guid.Empty) throw new InvalidOperationException("Thread id is required.");
            if (_role is null) throw new InvalidOperationException("Message role is required.");
            if (string.IsNullOrWhiteSpace(_authorId)) throw new InvalidOperationException("Author id is required.");
            if (string.IsNullOrWhiteSpace(_content)) throw new InvalidOperationException("Message content is required.");
            if (_createdAtUtc == default) throw new InvalidOperationException("Created timestamp is required.");

            return new ConversationMessage(_messageId.Value, _threadId.Value, _role.Value, _authorId, _content, _createdAtUtc, _turnId, _parentMessageId, _metadata);
        }
    }
}
