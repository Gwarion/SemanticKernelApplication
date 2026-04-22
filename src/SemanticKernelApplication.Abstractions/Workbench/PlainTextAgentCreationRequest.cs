namespace SemanticKernelApplication.Abstractions.Workbench;

/// <summary>
/// Requests creation of an agent from a natural-language description.
/// </summary>
/// <param name="Description">Free-form description of the desired agent.</param>
public sealed record PlainTextAgentCreationRequest(string Description);
