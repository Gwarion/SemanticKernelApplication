using Microsoft.EntityFrameworkCore;

namespace SemanticKernelApplication.Persistence;

/// <summary>
/// Wraps the pooled EF Core factory so repositories do not manage context creation directly.
/// </summary>
internal sealed class PooledLocalWorkbenchDbContextProvider(IDbContextFactory<LocalWorkbenchDbContext> contextFactory) : ILocalWorkbenchDbContextProvider
{
    public LocalWorkbenchDbContext CreateDbContext()
    {
        var context = contextFactory.CreateDbContext();
        context.Database.EnsureCreated();
        return context;
    }

    public async ValueTask<LocalWorkbenchDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        return context;
    }
}
