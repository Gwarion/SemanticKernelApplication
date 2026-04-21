using System.Collections.Concurrent;
using SemanticKernelApplication.Abstractions.Conversations;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationThread> _threads = new(StringComparer.OrdinalIgnoreCase);

    public Task<ConversationThread?> GetAsync(string threadId, CancellationToken cancellationToken = default)
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
