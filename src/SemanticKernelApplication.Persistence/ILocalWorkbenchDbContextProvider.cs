namespace SemanticKernelApplication.Persistence;

/// <summary>
/// Creates initialized <see cref="LocalWorkbenchDbContext"/> instances for repository operations.
/// </summary>
internal interface ILocalWorkbenchDbContextProvider
{
    /// <summary>
    /// Creates an initialized database context.
    /// </summary>
    LocalWorkbenchDbContext CreateDbContext();

    /// <summary>
    /// Creates an initialized database context asynchronously.
    /// </summary>
    ValueTask<LocalWorkbenchDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
