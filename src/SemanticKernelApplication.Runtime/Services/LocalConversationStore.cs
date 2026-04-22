using SemanticKernelApplication.Abstractions.Conversations;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class LocalConversationStore : IConversationStore
{
    private readonly IConversationThreadRepository _repository;

    public LocalConversationStore(IConversationThreadRepository repository) => _repository = repository;

    public Task<ConversationThread?> GetAsync(Guid threadId, CancellationToken cancellationToken = default) =>
        _repository.GetAsync(threadId, cancellationToken);

    public Task<ConversationThread> SaveAsync(ConversationThread thread, CancellationToken cancellationToken = default) =>
        _repository.SaveAsync(thread, cancellationToken);
}
