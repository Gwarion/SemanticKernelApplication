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
                        ["providerId"] = agentDefinition.ProviderId ?? string.Empty
                    });

                var executionResult = await _agentExecutor.ExecuteAsync(executionRequest, cancellationToken);
                var content = executionResult.Output ?? executionResult.Summary ?? $"{agentDefinition.Name} completed.";
                var message = new ConversationMessage(
                    Guid.NewGuid().ToString("N"),
                    thread.ThreadId,
                    ConversationMessageRole.Assistant,
                    agentDefinition.Id,
                    content,
                    DateTimeOffset.UtcNow);

                roundMessages.Add(message);
                summaries.Add($"{agentDefinition.Name}: {executionResult.Summary ?? executionResult.Output}");

                await PublishAsync(
                    ActivityKind.AgentExecution,
                    executionResult.Status == AgentExecutionStatus.Failed ? ActivityStatus.Failed : ActivityStatus.Completed,
                    agentDefinition.Name,
                    executionResult.Summary ?? content,
                    thread.ThreadId,
                    cancellationToken,
                    agentDefinition.Id);
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

    private ValueTask PublishAsync(
        ActivityKind kind,
        ActivityStatus status,
        string title,
        string message,
        string threadId,
        CancellationToken cancellationToken,
        string? agentId = null)
    {
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
                    AgentId: agentId)),
            cancellationToken);
    }
}
