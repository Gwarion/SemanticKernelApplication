using System.Collections.Concurrent;
using SemanticKernelApplication.Abstractions.Conversations;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, ConversationThread> _threads = [];

    public Task<ConversationThread?> GetAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _threads.TryGetValue(threadId, out var thread);
        return Task.FromResult(thread);
    }

    public Task<ConversationThread> SaveAsync(ConversationThread thread, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _threads[thread.ThreadId] = thread;
        return Task.FromResult(thread);
    }
}
