namespace SemanticKernelApplication.Persistence.Configuration;

/// <summary>
/// Configures where the local EF Core SQLite database is stored.
/// </summary>
public sealed class LocalPersistenceOptions
{
    /// <summary>
    /// Gets or sets the relative or absolute path to the local SQLite database.
    /// </summary>
    public string DatabasePath { get; set; } = ".appdata\\workbench.db";
}
