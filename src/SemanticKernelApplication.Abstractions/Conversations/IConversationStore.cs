namespace SemanticKernelApplication.Abstractions.Conversations;

/// <summary>
/// Persists conversation threads for later retrieval.
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Gets a conversation thread by identifier.
    /// </summary>
    /// <param name="threadId">Identifier of the thread to load.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<ConversationThread?> GetAsync(string threadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a conversation thread and returns the stored representation.
    /// </summary>
    /// <param name="thread">Thread to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<ConversationThread> SaveAsync(ConversationThread thread, CancellationToken cancellationToken = default);
}
