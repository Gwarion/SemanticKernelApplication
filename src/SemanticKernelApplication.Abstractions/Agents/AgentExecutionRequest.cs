namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Carries the input and metadata required to run an agent.
/// </summary>
/// <param name="OperationId">Unique identifier for the execution operation.</param>
/// <param name="Agent">Agent being executed.</param>
/// <param name="Input">Prompt or work item given to the agent.</param>
/// <param name="ConversationId">Optional conversation identifier associated with the request.</param>
/// <param name="CorrelationId">Optional correlation identifier shared with related work.</param>
/// <param name="Metadata">Optional execution metadata.</param>
/// <param name="RequestedAtUtc">Optional request timestamp.</param>
public sealed record AgentExecutionRequest(
    string OperationId,
    AgentReference Agent,
    string Input,
    string? ConversationId = null,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    DateTimeOffset? RequestedAtUtc = null);
