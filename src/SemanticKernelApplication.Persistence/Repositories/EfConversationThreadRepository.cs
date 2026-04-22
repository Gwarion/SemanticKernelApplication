using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Persistence.Entities;
using SemanticKernelApplication.Runtime.Services;

namespace SemanticKernelApplication.Persistence.Repositories;

internal sealed class EfConversationThreadRepository(ILocalWorkbenchDbContextProvider dbContextProvider) : IConversationThreadRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ConversationThread?> GetAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextProvider.CreateDbContextAsync(cancellationToken);
        var entity = await context.ConversationThreads
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ThreadId == threadId.ToString("N"), cancellationToken);

        return entity is null
            ? null
            : JsonSerializer.Deserialize<ConversationThread>(entity.PayloadJson, JsonOptions);
    }

    public async Task<ConversationThread> SaveAsync(ConversationThread thread, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextProvider.CreateDbContextAsync(cancellationToken);
        var entity = await context.ConversationThreads
            .SingleOrDefaultAsync(item => item.ThreadId == thread.ThreadId.ToString("N"), cancellationToken);

        if (entity is null)
        {
            entity = new ConversationThreadEntity { ThreadId = thread.ThreadId.ToString("N") };
            context.ConversationThreads.Add(entity);
        }

        entity.Title = thread.Title;
        entity.State = thread.State.ToString();
        entity.CreatedAtUtc = thread.CreatedAtUtc.ToString("O");
        entity.UpdatedAtUtc = (thread.UpdatedAtUtc ?? thread.CreatedAtUtc).ToString("O");
        entity.PayloadJson = JsonSerializer.Serialize(thread, JsonOptions);

        await context.SaveChangesAsync(cancellationToken);
        return thread;
    }
}
