namespace SemanticKernelApplication.Abstractions.Providers;

/// <summary>
/// Describes one selectable model inside a provider catalog.
/// </summary>
/// <param name="Id">Provider-specific model identifier.</param>
/// <param name="DisplayName">Name shown in the UI.</param>
/// <param name="IsDefault">Indicates whether the model is the default selection.</param>
public sealed record ModelDefinition(
    string Id,
    string DisplayName,
    bool IsDefault = false);
