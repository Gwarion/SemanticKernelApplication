namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Configures where the local workbench database is stored.
/// </summary>
public sealed class LocalWorkbenchStoreOptions
{
    /// <summary>
    /// Gets or sets the relative or absolute path to the local SQLite database.
    /// </summary>
    public string DatabasePath { get; set; } = ".appdata\\workbench.db";
}
