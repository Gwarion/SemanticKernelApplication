using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;

namespace SemanticKernelApplication.Abstractions.Orchestration;

public enum CoordinationMode
{
    Sequential,
    RoundRobin,
    Facilitated,
    Custom
}

public sealed record CoordinationPolicy(
    CoordinationMode Mode,
    int MaxRounds = 1,
    bool StopWhenAnyAgentRequestsCompletion = true,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record CoordinatorDefinition(
    string Id,
    string Name,
    string Description,
    CoordinationPolicy Policy,
    AgentInstructionSet Instructions,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record CoordinationRequest(
    string OperationId,
    CoordinatorDefinition Coordinator,
    ConversationThread Thread,
    IReadOnlyList<AgentReference> Agents,
    string Objective,
    IReadOnlyDictionary<string, string>? Metadata = null,
    DateTimeOffset? RequestedAtUtc = null);

public sealed record CoordinationRound(
    int RoundNumber,
    IReadOnlyList<ConversationMessage> Messages,
    UsageSnapshot? Usage = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record CoordinationResult(
    string OperationId,
    ActivityStatus Status,
    ConversationThread Thread,
    IReadOnlyList<CoordinationRound> Rounds,
    string? Summary = null,
    string? CompletionReason = null,
    UsageSnapshot? Usage = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public interface ICoordinatorOrchestrator
{
    Task<CoordinationResult> ExecuteAsync(
        CoordinationRequest request,
        CancellationToken cancellationToken = default);
}
