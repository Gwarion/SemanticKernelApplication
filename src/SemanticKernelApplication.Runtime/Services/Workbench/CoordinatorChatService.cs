using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Orchestration;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Runtime.Services.Agents;
using SemanticKernelApplication.Tools.Configuration;
using SemanticKernelApplication.Tools.Providers;
using Microsoft.Extensions.Options;

namespace SemanticKernelApplication.Runtime.Services.Workbench;

public sealed class CoordinatorChatService : ICoordinatorChatService
{
    private const string CoordinatorId = "coordinator";

    private readonly IAgentCreationService _agentCreationService;
    private readonly IAgentDefinitionStore _agentDefinitionStore;
    private readonly IConversationStore _conversationStore;
    private readonly ICoordinatorOrchestrator _orchestrator;
    private readonly IActivitySink _activitySink;
    private readonly InMemoryActivityLog _activityLog;
    private readonly IProviderSessionConfiguration _providerSessionConfiguration;
    private readonly AgentProviderOptions _providerOptions;
    private readonly IWorkbenchSnapshotFactory _snapshotFactory;

    public CoordinatorChatService(
        IAgentCreationService agentCreationService,
        IAgentDefinitionStore agentDefinitionStore,
        IConversationStore conversationStore,
        ICoordinatorOrchestrator orchestrator,
        IActivitySink activitySink,
        InMemoryActivityLog activityLog,
        IProviderSessionConfiguration providerSessionConfiguration,
        IOptions<AgentProviderOptions> providerOptions,
        IWorkbenchSnapshotFactory snapshotFactory)
    {
        _agentCreationService = agentCreationService;
        _agentDefinitionStore = agentDefinitionStore;
        _conversationStore = conversationStore;
        _orchestrator = orchestrator;
        _activitySink = activitySink;
        _activityLog = activityLog;
        _providerSessionConfiguration = providerSessionConfiguration;
        _providerOptions = providerOptions.Value;
        _snapshotFactory = snapshotFactory;
    }

    public async Task<CoordinatorChatResponse> SendAsync(
        CoordinatorChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        var trimmedMessage = request.Message.Trim();
        var thread = await GetOrCreateConversationAsync(request.ConversationId, cancellationToken);
        var userMessage = CreateMessage(thread.ThreadId, ConversationMessageRole.User, "user", trimmedMessage);

        if (CoordinatorIntentParser.TryExtractAgentDescription(trimmedMessage, out var agentDescription))
        {
            return await HandleAgentCreationAsync(thread, userMessage, agentDescription, cancellationToken);
        }

        return await HandleCoordinationAsync(thread, userMessage, trimmedMessage, cancellationToken);
    }

    private async Task<CoordinatorChatResponse> HandleAgentCreationAsync(
        ConversationThread thread,
        ConversationMessage userMessage,
        string agentDescription,
        CancellationToken cancellationToken)
    {
        var agent = await _agentCreationService.CreateFromTextAsync(
            new PlainTextAgentCreationRequest(agentDescription),
            cancellationToken);
        var providerId = _providerSessionConfiguration.GetConfiguration(_providerOptions.Providers).SelectedProviderId;

        var replyText =
            $"I created {agent.Name} and added it to the agent studio. It will participate using the current global model setting ({providerId}).";
        var replyMessage = CreateMessage(thread.ThreadId, ConversationMessageRole.Assistant, CoordinatorId, replyText);
        var participantThread = EnsureAgentParticipant(thread, agent);
        var updatedThread = await SaveThreadAsync(participantThread, [userMessage, replyMessage], cancellationToken);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                updatedThread.ThreadId,
                new ActivityLogEntry(
                    0,
                    ActivityKind.Coordination,
                    ActivityStatus.Completed,
                    ActivitySeverity.Information,
                    "Coordinator created an agent",
                    replyText,
                    DateTimeOffset.UtcNow,
                    ConversationId: updatedThread.ThreadId,
                    AgentId: agent.Id)),
            cancellationToken);

        return new CoordinatorChatResponse(
            updatedThread.ThreadId,
            replyText,
            new CoordinationResult(
                Guid.NewGuid().ToString("N"),
                ActivityStatus.Completed,
                updatedThread,
                [],
                replyText,
                "Agent created by coordinator"),
            _activityLog.GetRecent(40));
    }

    private async Task<CoordinatorChatResponse> HandleCoordinationAsync(
        ConversationThread thread,
        ConversationMessage userMessage,
        string trimmedMessage,
        CancellationToken cancellationToken)
    {
        var savedThread = await SaveThreadAsync(thread, [userMessage], cancellationToken);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                savedThread.ThreadId,
                new ActivityLogEntry(
                    0,
                    ActivityKind.Message,
                    ActivityStatus.Completed,
                    ActivitySeverity.Information,
                    "Coordinator received a new user message",
                    trimmedMessage,
                    DateTimeOffset.UtcNow,
                    ConversationId: savedThread.ThreadId)),
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
                savedThread,
                agents.Select(agent => new AgentReference(agent.Id, agent.Name, agent.Kind)).ToArray(),
                trimmedMessage),
            cancellationToken);

        var replyText = BuildCoordinatorSummary(result);
        var replyMessage = CreateMessage(result.Thread.ThreadId, ConversationMessageRole.Assistant, CoordinatorId, replyText);
        var updatedThread = await SaveThreadAsync(result.Thread, [replyMessage], cancellationToken);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                updatedThread.ThreadId,
                new ActivityLogEntry(
                    0,
                    ActivityKind.Coordination,
                    ActivityStatus.Completed,
                    ActivitySeverity.Information,
                    "Coordinator reply ready",
                    replyText,
                    DateTimeOffset.UtcNow,
                    ConversationId: updatedThread.ThreadId)),
            cancellationToken);

        return new CoordinatorChatResponse(
            updatedThread.ThreadId,
            replyText,
            result with { Thread = updatedThread },
            _activityLog.GetRecent(40));
    }

    private async Task<ConversationThread> GetOrCreateConversationAsync(
        string? conversationId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            var existing = await _conversationStore.GetAsync(conversationId, cancellationToken);
            if (existing is not null)
            {
                _snapshotFactory.SetActiveConversation(existing.ThreadId);
                return existing;
            }
        }

        var snapshot = await _snapshotFactory.CreateAsync(cancellationToken);
        if (snapshot.ActiveConversation is not null)
        {
            return snapshot.ActiveConversation;
        }

        var participants = new List<ConversationParticipant>
        {
            new("user", "You", ConversationParticipantKind.User),
            new(CoordinatorId, "Coordinator", ConversationParticipantKind.Coordinator)
        };

        var agents = await _agentDefinitionStore.ListAsync(cancellationToken);
        participants.AddRange(
            agents.Select(agent => new ConversationParticipant(agent.Id, agent.Name, ConversationParticipantKind.Agent, agent.Id)));

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
        _snapshotFactory.SetActiveConversation(thread.ThreadId);
        return thread;
    }

    private async Task<ConversationThread> SaveThreadAsync(
        ConversationThread thread,
        IReadOnlyList<ConversationMessage> newMessages,
        CancellationToken cancellationToken)
    {
        var updatedThread = thread with
        {
            Messages = [.. thread.Messages, .. newMessages],
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        updatedThread = await _conversationStore.SaveAsync(updatedThread, cancellationToken);
        _snapshotFactory.SetActiveConversation(updatedThread.ThreadId);
        return updatedThread;
    }

    private static ConversationMessage CreateMessage(
        string threadId,
        ConversationMessageRole role,
        string authorId,
        string content)
    {
        return new ConversationMessage(
            Guid.NewGuid().ToString("N"),
            threadId,
            role,
            authorId,
            content,
            DateTimeOffset.UtcNow);
    }

    private static ConversationThread EnsureAgentParticipant(ConversationThread thread, AgentDefinition agent)
    {
        if (thread.Participants.Any(participant => string.Equals(participant.ParticipantId, agent.Id, StringComparison.Ordinal)))
        {
            return thread;
        }

        return thread with
        {
            Participants =
            [
                .. thread.Participants,
                new ConversationParticipant(agent.Id, agent.Name, ConversationParticipantKind.Agent, agent.Id)
            ]
        };
    }

    private static string BuildCoordinatorSummary(CoordinationResult result)
    {
        if (result.Rounds.Count == 0)
        {
            return "No specialists were available. Create an agent from the studio panel and I'll route future work through it.";
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
