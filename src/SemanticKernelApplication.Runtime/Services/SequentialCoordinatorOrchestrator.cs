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
        var turnId = TryGetGuid(request.Metadata, "turnId");

        await PublishAsync(ActivityKind.Workflow, ActivityStatus.Running, "Coordinator started", request.Objective, thread.ThreadId, cancellationToken);

        if (request.Agents.Count == 0)
        {
            await PublishAsync(ActivityKind.Coordination, ActivityStatus.Completed, "No agents available", "Create an agent from the studio panel to start coordinating work.", thread.ThreadId, cancellationToken);

            return CoordinationResult.Builder
                .WithOperationId(request.OperationId)
                .WithStatus(ActivityStatus.Completed)
                .WithThread(thread)
                .WithRounds([])
                .WithSummary("No agents were available to run.")
                .WithCompletionReason("No enabled agents")
                .Build();
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

                await PublishAsync(
                    ActivityKind.AgentExecution,
                    ActivityStatus.Running,
                    $"Running {agentDefinition.Name}",
                    agentDefinition.Description,
                    thread.ThreadId,
                    cancellationToken,
                    agentDefinition.Id);

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
                var message = ConversationMessage.Builder
                    .WithMessageId(Guid.NewGuid())
                    .WithThreadId(thread.ThreadId)
                    .WithRole(ConversationMessageRole.Assistant)
                    .WithAuthorId(agentDefinition.Id.ToString("N"))
                    .WithContent(content)
                    .WithCreatedAtUtc(DateTimeOffset.UtcNow)
                    .WithTurnId(turnId)
                    .Build();

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
                thread = thread
                    .ToBuilder()
                    .WithMessages([.. thread.Messages, .. roundMessages])
                    .WithUpdatedAtUtc(DateTimeOffset.UtcNow)
                    .Build();

                thread = await _conversationStore.SaveAsync(thread, cancellationToken);
                rounds.Add(new CoordinationRound(round, roundMessages));
            }
        }

        var summary = summaries.Count == 0
            ? "The coordinator did not receive any specialist output."
            : string.Join(Environment.NewLine, summaries);

        await PublishAsync(ActivityKind.Workflow, ActivityStatus.Completed, "Coordinator completed", summary, thread.ThreadId, cancellationToken);

        return CoordinationResult.Builder
            .WithOperationId(request.OperationId)
            .WithStatus(ActivityStatus.Completed)
            .WithThread(thread)
            .WithRounds(rounds)
            .WithSummary(summary)
            .WithCompletionReason("Completed")
            .Build();
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
        Guid threadId,
        CancellationToken cancellationToken)
    {
        return PublishAsyncCore(kind, status, title, message, threadId, cancellationToken, agentId: null, failureReason: null, metadata: null);
    }

    private ValueTask PublishAsync(
        ActivityKind kind,
        ActivityStatus status,
        string title,
        string message,
        Guid threadId,
        CancellationToken cancellationToken,
        Guid agentId)
    {
        return PublishAsyncCore(kind, status, title, message, threadId, cancellationToken, agentId, failureReason: null, metadata: null);
    }

    private ValueTask PublishAsync(
        ActivityKind kind,
        ActivityStatus status,
        string title,
        string message,
        Guid threadId,
        CancellationToken cancellationToken,
        Guid? agentId,
        string? failureReason,
        IReadOnlyDictionary<string, string>? metadata)
    {
        return PublishAsyncCore(kind, status, title, message, threadId, cancellationToken, agentId, failureReason, metadata);
    }

    private ValueTask PublishAsyncCore(
        ActivityKind kind,
        ActivityStatus status,
        string title,
        string message,
        Guid threadId,
        CancellationToken cancellationToken,
        Guid? agentId,
        string? failureReason,
        IReadOnlyDictionary<string, string>? metadata)
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
                threadId.ToString("N"),
                ActivityLogEntry.Builder
                    .WithSequence(0)
                    .WithKind(kind)
                    .WithStatus(status)
                    .WithSeverity(status == ActivityStatus.Failed ? ActivitySeverity.Error : ActivitySeverity.Information)
                    .WithTitle(title)
                    .WithMessage(message)
                    .WithTimestampUtc(DateTimeOffset.UtcNow)
                    .WithConversationId(threadId)
                    .WithAgentId(agentId)
                    .WithMetadata(activityMetadata)
                    .Build()),
            cancellationToken);
    }

    private static Guid? TryGetGuid(IReadOnlyDictionary<string, string>? metadata, string key)
    {
        if (metadata is null || !metadata.TryGetValue(key, out var rawValue) || !Guid.TryParse(rawValue, out var value))
        {
            return null;
        }

        return value;
    }
}
