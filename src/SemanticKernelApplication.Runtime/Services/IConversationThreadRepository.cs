using SemanticKernelApplication.Abstractions.Conversations;

namespace SemanticKernelApplication.Runtime.Services;

/// <summary>
/// Provides persistence operations for conversation thread storage.
/// </summary>
public interface IConversationThreadRepository
{
    /// <summary>
    /// Loads a conversation thread by identifier.
    /// </summary>
    Task<ConversationThread?> GetAsync(Guid threadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a conversation thread and returns the stored representation.
    /// </summary>
    Task<ConversationThread> SaveAsync(ConversationThread thread, CancellationToken cancellationToken = default);
}
