using System.Collections.ObjectModel;

namespace SemanticKernelApplication.Abstractions.Agents;

public enum AgentKind
{
    Assistant,
    Coordinator,
    UserDefined,
    ToolProxy
}

public enum AgentExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed record AgentInstructionSet(
    string SystemPrompt,
    IReadOnlyList<string>? Goals = null,
    IReadOnlyList<string>? Constraints = null,
    IReadOnlyDictionary<string, string>? Variables = null)
{
    public static AgentInstructionSet Empty { get; } = new(string.Empty);
}

public sealed record AgentCapability(
    string Name,
    string Description,
    bool IsEnabled = true,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record AgentDefinition(
    string Id,
    string Name,
    AgentKind Kind,
    string Description,
    AgentInstructionSet Instructions,
    string? ProviderId = null,
    IReadOnlyList<AgentCapability>? Capabilities = null,
    IReadOnlyCollection<string>? Tags = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    string? Version = null,
    bool IsEnabled = true,
    DateTimeOffset? CreatedAtUtc = null,
    DateTimeOffset? UpdatedAtUtc = null)
{
    public IReadOnlyList<AgentCapability> CapabilitiesOrEmpty { get; } =
        new ReadOnlyCollection<AgentCapability>((Capabilities ?? []).ToArray());

    public IReadOnlyCollection<string> TagsOrEmpty { get; } =
        new ReadOnlyCollection<string>((Tags ?? []).ToArray());

    public DateTimeOffset CreatedAtUtcOrNow { get; } = CreatedAtUtc ?? DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtcOrNow { get; } = UpdatedAtUtc ?? DateTimeOffset.UtcNow;
}

public sealed record AgentReference(
    string AgentId,
    string DisplayName,
    AgentKind Kind,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record AgentExecutionRequest(
    string OperationId,
    AgentReference Agent,
    string Input,
    string? ConversationId = null,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    DateTimeOffset? RequestedAtUtc = null);

public sealed record AgentExecutionResult(
    string OperationId,
    AgentExecutionStatus Status,
    string? Output = null,
    string? Summary = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    DateTimeOffset? StartedAtUtc = null,
    DateTimeOffset? CompletedAtUtc = null,
    string? FailureReason = null);

public interface IAgentDefinitionStore
{
    Task<IReadOnlyList<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default);

    Task<AgentDefinition?> GetAsync(string agentId, CancellationToken cancellationToken = default);

    Task<AgentDefinition> UpsertAsync(AgentDefinition definition, CancellationToken cancellationToken = default);
}

public interface IAgentExecutor
{
    Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionRequest request,
        CancellationToken cancellationToken = default);
}
