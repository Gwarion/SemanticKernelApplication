namespace SemanticKernelApplication.Abstractions.Orchestration;

/// <summary>
/// Captures the rules that guide a coordination run.
/// </summary>
/// <param name="Mode">Coordination strategy to apply.</param>
/// <param name="MaxRounds">Maximum number of coordination rounds.</param>
/// <param name="Metadata">Optional policy metadata.</param>
public sealed record CoordinationPolicy(
    CoordinationMode Mode,
    int MaxRounds = 1,
    IReadOnlyDictionary<string, string>? Metadata = null);
