namespace SemanticKernelApplication.Abstractions.Activities;

/// <summary>
/// Captures lightweight usage metrics for a model call or orchestration step.
/// </summary>
/// <param name="InputTokens">Number of input tokens consumed.</param>
/// <param name="OutputTokens">Number of output tokens produced.</param>
/// <param name="TotalTokens">Total tokens associated with the operation.</param>
/// <param name="Duration">Optional elapsed execution time.</param>
/// <param name="Metadata">Optional provider-specific usage details.</param>
public sealed record UsageSnapshot(
    int InputTokens = 0,
    int OutputTokens = 0,
    int TotalTokens = 0,
    TimeSpan? Duration = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
