using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Orchestration;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class SequentialCoordinatorOrchestrator : ICoordinatorOrchestrator
{
    private readonly IConversationStore _conversationStore;
    private readonly IAgentDefinitionStore _agentDefinitionStore;
    private readonly IAgentExecutor _agentExecutor;
    private readonly IActivitySink _activitySink;

    public SequentialCoordinatorOrchestrator(
        IConversationStore conversationStore,
        IAgentDefinitionStore agentDefinitionStore,
        IAgentExecutor agentExecutor,
        IActivitySink activitySink)
    {
        _conversationStore = conversationStore;
        _agentDefinitionStore = agentDefinitionStore;
        _agentExecutor = agentExecutor;
        _activitySink = activitySink;
    }

    public async Task<CoordinationResult> ExecuteAsync(CoordinationRequest request, CancellationToken cancellationToken = default)
    {
        var thread = request.Thread;
        var rounds = new List<CoordinationRound>();
        var summaries = new List<string>();
        var turnId = request.Metadata?.GetValueOrDefault("turnId");

        await PublishAsync(ActivityKind.Workflow, ActivityStatus.Running, "Coordinator started", request.Objective, thread.ThreadId, cancellationToken);

        if (request.Agents.Count == 0)
        {
            await PublishAsync(ActivityKind.Coordination, ActivityStatus.Completed, "No agents available", "Create an agent from the studio panel to start coordinating work.", thread.ThreadId, cancellationToken);

            return new CoordinationResult(
                request.OperationId,
                ActivityStatus.Completed,
                thread,
                [],
                "No agents were available to run.",
                "No enabled agents");
        }

        for (var round = 1; round <= Math.Max(1, request.Coordinator.Policy.MaxRounds); round++)
        {
            var roundMessages = new List<ConversationMessage>();

            foreach (var agentReference in request.Agents)
            {
                var agentDefinition = await _agentDefinitionStore.GetAsync(agentReference.AgentId, cancellationToken);
                if (agentDefinition is null)
                {
                    continue;
                }

                await PublishAsync(ActivityKind.AgentExecution, ActivityStatus.Running, $"Running {agentDefinition.Name}", agentDefinition.Description, thread.ThreadId, cancellationToken, agentDefinition.Id);

                var executionRequest = new AgentExecutionRequest(
                    request.OperationId,
                    agentReference,
                    request.Objective,
                    thread.ThreadId,
                    Metadata: new Dictionary<string, string>
                    {
                        ["agentDescription"] = agentDefinition.Description,
                        ["providerId"] = agentDefinition.ProviderId ?? string.Empty,
                        ["agentSystemPrompt"] = agentDefinition.Instructions.SystemPrompt,
                        ["conversationHistory"] = string.Join(
                            Environment.NewLine,
                            thread.Messages.TakeLast(10).Select(message => $"{ResolveParticipantName(thread, message.AuthorId)}: {message.Content}"))
                    });

                var executionResult = await _agentExecutor.ExecuteAsync(executionRequest, cancellationToken);
                var content = executionResult.Status == AgentExecutionStatus.Failed
                    ? $"{agentDefinition.Name} could not complete that step. Check the exceptions panel for details."
                    : executionResult.Output ?? executionResult.Summary ?? $"{agentDefinition.Name} completed.";
                var message = new ConversationMessage(
                    Guid.NewGuid().ToString("N"),
                    thread.ThreadId,
                    ConversationMessageRole.Assistant,
                    agentDefinition.Id,
                    content,
                    DateTimeOffset.UtcNow,
                    turnId);

                roundMessages.Add(message);
                summaries.Add($"{agentDefinition.Name}: {executionResult.Summary ?? executionResult.Output}");

                await PublishAsync(
                    ActivityKind.AgentExecution,
                    executionResult.Status == AgentExecutionStatus.Failed ? ActivityStatus.Failed : ActivityStatus.Completed,
                    agentDefinition.Name,
                    executionResult.Status == AgentExecutionStatus.Failed
                        ? executionResult.FailureReason ?? executionResult.Summary ?? content
                        : executionResult.Summary ?? content,
                    thread.ThreadId,
                    cancellationToken,
                    agentDefinition.Id,
                    executionResult.FailureReason,
                    executionResult.Metadata);
            }

            if (roundMessages.Count > 0)
            {
                thread = thread with
                {
                    Messages = [.. thread.Messages, .. roundMessages],
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };

                thread = await _conversationStore.SaveAsync(thread, cancellationToken);
                rounds.Add(new CoordinationRound(round, roundMessages));
            }
        }

        var summary = summaries.Count == 0
            ? "The coordinator did not receive any specialist output."
            : string.Join(Environment.NewLine, summaries);

        await PublishAsync(ActivityKind.Workflow, ActivityStatus.Completed, "Coordinator completed", summary, thread.ThreadId, cancellationToken);

        return new CoordinationResult(
            request.OperationId,
            ActivityStatus.Completed,
            thread,
            rounds,
            summary,
            "Completed");
    }

    private static string ResolveParticipantName(ConversationThread thread, string authorId)
    {
        return thread.Participants.FirstOrDefault(participant => participant.ParticipantId == authorId)?.DisplayName
            ?? authorId;
    }

    private ValueTask PublishAsync(
        ActivityKind kind,
        ActivityStatus status,
        string title,
        string message,
        string threadId,
        CancellationToken cancellationToken,
        string? agentId = null,
        string? failureReason = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Dictionary<string, string>? activityMetadata = null;

        if (metadata is not null || !string.IsNullOrWhiteSpace(failureReason))
        {
            activityMetadata = new Dictionary<string, string>(StringComparer.Ordinal);

            if (metadata is not null)
            {
                foreach (var pair in metadata)
                {
                    activityMetadata[pair.Key] = pair.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                activityMetadata["failureReason"] = failureReason;
            }
        }

        return _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                threadId,
                new ActivityLogEntry(
                    0,
                    kind,
                    status,
                    status == ActivityStatus.Failed ? ActivitySeverity.Error : ActivitySeverity.Information,
                    title,
                    message,
                    DateTimeOffset.UtcNow,
                    ConversationId: threadId,
                    AgentId: agentId,
                    Metadata: activityMetadata)),
            cancellationToken);
    }
}
