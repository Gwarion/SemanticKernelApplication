using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Orchestration;
using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Providers;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class AgentWorkbenchService : IAgentWorkbenchService
{
    private const string CoordinatorId = "coordinator";
    private readonly IAgentDefinitionStore _agentDefinitionStore;
    private readonly IConversationStore _conversationStore;
    private readonly IAiProviderCatalog _providerCatalog;
    private readonly PlainTextAgentDefinitionFactory _definitionFactory;
    private readonly ICoordinatorOrchestrator _orchestrator;
    private readonly IActivitySink _activitySink;
    private readonly InMemoryActivityLog _activityLog;
    private string? _activeConversationId;

    public AgentWorkbenchService(
        IAgentDefinitionStore agentDefinitionStore,
        IConversationStore conversationStore,
        IAiProviderCatalog providerCatalog,
        PlainTextAgentDefinitionFactory definitionFactory,
        ICoordinatorOrchestrator orchestrator,
        IActivitySink activitySink,
        InMemoryActivityLog activityLog)
    {
        _agentDefinitionStore = agentDefinitionStore;
        _conversationStore = conversationStore;
        _providerCatalog = providerCatalog;
        _definitionFactory = definitionFactory;
        _orchestrator = orchestrator;
        _activitySink = activitySink;
        _activityLog = activityLog;
    }

    public async Task<AgentDefinition> CreateAgentFromTextAsync(
        PlainTextAgentCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Agent description is required.", nameof(request));
        }

        var definition = _definitionFactory.Create(request);
        definition = await _agentDefinitionStore.UpsertAsync(definition, cancellationToken);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                "workbench",
                new ActivityLogEntry(
                    0,
                    ActivityKind.Status,
                    ActivityStatus.Completed,
                    ActivitySeverity.Information,
                    "Agent created",
                    $"{definition.Name} is ready for coordinator assignments.",
                    DateTimeOffset.UtcNow,
                    AgentId: definition.Id,
                    Metadata: new Dictionary<string, string> { ["providerId"] = definition.ProviderId ?? string.Empty })),
            cancellationToken);

        return definition;
    }

    public async Task<WorkbenchSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _agentDefinitionStore.ListAsync(cancellationToken);
        var conversation = _activeConversationId is null ? null : await _conversationStore.GetAsync(_activeConversationId, cancellationToken);
        var providers = _providerCatalog.GetProviders();
        var activity = _activityLog.GetRecent(80);

        return new WorkbenchSnapshot(agents, providers, conversation, activity);
    }

    public async Task<CoordinatorChatResponse> SendCoordinatorMessageAsync(
        CoordinatorChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        var thread = await GetOrCreateConversationAsync(request.ConversationId, cancellationToken);
        var userMessage = new ConversationMessage(
            Guid.NewGuid().ToString("N"),
            thread.ThreadId,
            ConversationMessageRole.User,
            "user",
            request.Message.Trim(),
            DateTimeOffset.UtcNow);

        thread = thread with
        {
            Messages = [.. thread.Messages, userMessage],
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        thread = await _conversationStore.SaveAsync(thread, cancellationToken);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                thread.ThreadId,
                new ActivityLogEntry(
                    0,
                    ActivityKind.Message,
                    ActivityStatus.Completed,
                    ActivitySeverity.Information,
                    "Coordinator received a new user message",
                    request.Message.Trim(),
                    DateTimeOffset.UtcNow,
                    ConversationId: thread.ThreadId)),
            cancellationToken);

        var agents = await _agentDefinitionStore.ListAsync(cancellationToken);
        var coordinator = new CoordinatorDefinition(
            CoordinatorId,
            "Coordinator",
            "Routes user goals to the current bench of agents and summarizes public progress.",
            new CoordinationPolicy(CoordinationMode.Sequential, 1),
            new AgentInstructionSet("Coordinate agents and summarize their visible progress for the user."));

        var result = await _orchestrator.ExecuteAsync(
            new CoordinationRequest(
                Guid.NewGuid().ToString("N"),
                coordinator,
                thread,
                agents.Select(agent => new AgentReference(agent.Id, agent.Name, agent.Kind)).ToArray(),
                request.Message.Trim()),
            cancellationToken);

        var coordinatorMessageText = BuildCoordinatorSummary(result);
        var coordinatorMessage = new ConversationMessage(
            Guid.NewGuid().ToString("N"),
            result.Thread.ThreadId,
            ConversationMessageRole.Assistant,
            CoordinatorId,
            coordinatorMessageText,
            DateTimeOffset.UtcNow);

        var updatedThread = result.Thread with
        {
            Messages = [.. result.Thread.Messages, coordinatorMessage],
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        updatedThread = await _conversationStore.SaveAsync(updatedThread, cancellationToken);
        _activeConversationId = updatedThread.ThreadId;

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                updatedThread.ThreadId,
                new ActivityLogEntry(
                    0,
                    ActivityKind.Coordination,
                    ActivityStatus.Completed,
                    ActivitySeverity.Information,
                    "Coordinator reply ready",
                    coordinatorMessageText,
                    DateTimeOffset.UtcNow,
                    ConversationId: updatedThread.ThreadId)),
            cancellationToken);

        return new CoordinatorChatResponse(
            updatedThread.ThreadId,
            coordinatorMessageText,
            result with { Thread = updatedThread },
            _activityLog.GetRecent(40));
    }

    private async Task<ConversationThread> GetOrCreateConversationAsync(string? conversationId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            var existing = await _conversationStore.GetAsync(conversationId, cancellationToken);
            if (existing is not null)
            {
                _activeConversationId = existing.ThreadId;
                return existing;
            }
        }

        if (!string.IsNullOrWhiteSpace(_activeConversationId))
        {
            var active = await _conversationStore.GetAsync(_activeConversationId, cancellationToken);
            if (active is not null)
            {
                return active;
            }
        }

        var participants = new List<ConversationParticipant>
        {
            new("user", "You", ConversationParticipantKind.User),
            new(CoordinatorId, "Coordinator", ConversationParticipantKind.Coordinator)
        };

        var agents = await _agentDefinitionStore.ListAsync(cancellationToken);
        participants.AddRange(agents.Select(agent => new ConversationParticipant(agent.Id, agent.Name, ConversationParticipantKind.Agent, agent.Id)));

        var thread = new ConversationThread(
            Guid.NewGuid().ToString("N"),
            "Coordinator session",
            ConversationState.Active,
            participants,
            [],
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        thread = await _conversationStore.SaveAsync(thread, cancellationToken);
        _activeConversationId = thread.ThreadId;
        return thread;
    }

    private static string BuildCoordinatorSummary(CoordinationResult result)
    {
        if (result.Rounds.Count == 0)
        {
            return "No specialists were available. Create an agent from the studio panel and I’ll route future work through it.";
        }

        var latestMessages = result.Rounds
            .SelectMany(round => round.Messages)
            .TakeLast(4)
            .Select(message => $"- {message.AuthorId}: {message.Content}");

        return $$"""
            I coordinated the current bench and collected the latest visible progress:
            {{string.Join(Environment.NewLine, latestMessages)}}

            Summary:
            {{result.Summary ?? "The specialists completed their pass."}}
            """;
    }
}
