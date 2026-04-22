using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Orchestration;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Runtime.Services.Agents;
using SemanticKernelApplication.Tools.Providers;

namespace SemanticKernelApplication.Runtime.Services.Workbench;

public sealed class CoordinatorChatService : ICoordinatorChatService
{
    private const string CoordinatorId = "coordinator";

    private readonly IAgentCreationService _agentCreationService;
    private readonly IAgentDefinitionStore _agentDefinitionStore;
    private readonly IConversationStore _conversationStore;
    private readonly ICoordinatorOrchestrator _orchestrator;
    private readonly IAgentExecutor _agentExecutor;
    private readonly IActivitySink _activitySink;
    private readonly InMemoryActivityLog _activityLog;
    private readonly IProviderSessionConfiguration _providerSessionConfiguration;
    private readonly IWorkbenchSnapshotFactory _snapshotFactory;

    public CoordinatorChatService(
        IAgentCreationService agentCreationService,
        IAgentDefinitionStore agentDefinitionStore,
        IConversationStore conversationStore,
        ICoordinatorOrchestrator orchestrator,
        IAgentExecutor agentExecutor,
        IActivitySink activitySink,
        InMemoryActivityLog activityLog,
        IProviderSessionConfiguration providerSessionConfiguration,
        IWorkbenchSnapshotFactory snapshotFactory)
    {
        _agentCreationService = agentCreationService;
        _agentDefinitionStore = agentDefinitionStore;
        _conversationStore = conversationStore;
        _orchestrator = orchestrator;
        _agentExecutor = agentExecutor;
        _activitySink = activitySink;
        _activityLog = activityLog;
        _providerSessionConfiguration = providerSessionConfiguration;
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
        var turn = CreateTurn(thread, "user", trimmedMessage);
        var userMessage = CreateMessage(thread.ThreadId, ConversationMessageRole.User, "user", trimmedMessage, turn.TurnId);

        if (CoordinatorIntentParser.TryExtractAgentDescription(trimmedMessage, out var agentDescription))
        {
            return await HandleAgentCreationAsync(thread, turn, userMessage, agentDescription, cancellationToken);
        }

        return await HandleCoordinationAsync(thread, turn, userMessage, trimmedMessage, cancellationToken);
    }

    private async Task<CoordinatorChatResponse> HandleAgentCreationAsync(
        ConversationThread thread,
        ConversationTurn turn,
        ConversationMessage userMessage,
        string agentDescription,
        CancellationToken cancellationToken)
    {
        var agent = await _agentCreationService.CreateFromTextAsync(
            new PlainTextAgentCreationRequest(agentDescription),
            cancellationToken);
        var configuration = _providerSessionConfiguration.GetConfiguration();

        var replyText =
            $"I created {agent.Name} and added it to the agent studio. It will use {configuration.SelectedProviderId} / {configuration.SelectedModelId}, and you can start delegating work to it right away.";
        var replyMessage = CreateMessage(thread.ThreadId, ConversationMessageRole.Assistant, CoordinatorId, replyText, turn.TurnId, userMessage.MessageId);
        var participantThread = EnsureAgentParticipant(thread, agent);
        var updatedThread = await SaveThreadAsync(participantThread, turn, [userMessage, replyMessage], cancellationToken);

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
        ConversationTurn turn,
        ConversationMessage userMessage,
        string trimmedMessage,
        CancellationToken cancellationToken)
    {
        var savedThread = await SaveThreadAsync(thread, turn, [userMessage], cancellationToken);

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
                    trimmedMessage,
                    Metadata: new Dictionary<string, string>
                    {
                        ["turnId"] = turn.TurnId
                    }),
            cancellationToken);

        var replyText = await BuildCoordinatorReplyAsync(savedThread, result, trimmedMessage, cancellationToken);
        var replyMessage = CreateMessage(result.Thread.ThreadId, ConversationMessageRole.Assistant, CoordinatorId, replyText, turn.TurnId, userMessage.MessageId);
        var updatedThread = await SaveThreadAsync(result.Thread, turn with { CompletedAtUtc = DateTimeOffset.UtcNow }, [replyMessage], cancellationToken);

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

        var snapshot = await _snapshotFactory.CreateAsync(conversationId, cancellationToken);
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
        ConversationTurn turn,
        IReadOnlyList<ConversationMessage> newMessages,
        CancellationToken cancellationToken)
    {
        var updatedThread = thread with
        {
            Turns = UpsertTurn(thread.Turns, turn),
            Messages = [.. thread.Messages, .. newMessages],
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        updatedThread = await _conversationStore.SaveAsync(updatedThread, cancellationToken);
        _snapshotFactory.SetActiveConversation(updatedThread.ThreadId);
        return updatedThread;
    }

    private static ConversationTurn CreateTurn(ConversationThread thread, string initiatedByParticipantId, string goal)
    {
        return new ConversationTurn(
            Guid.NewGuid().ToString("N"),
            thread.ThreadId,
            thread.Turns.Count + 1,
            initiatedByParticipantId,
            DateTimeOffset.UtcNow,
            Goal: goal);
    }

    private static IReadOnlyList<ConversationTurn> UpsertTurn(IReadOnlyList<ConversationTurn> existingTurns, ConversationTurn turn)
    {
        var remainingTurns = existingTurns.Where(item => !string.Equals(item.TurnId, turn.TurnId, StringComparison.Ordinal)).ToArray();
        return [.. remainingTurns, turn];
    }

    private static ConversationMessage CreateMessage(
        string threadId,
        ConversationMessageRole role,
        string authorId,
        string content,
        string? turnId = null,
        string? parentMessageId = null)
    {
        return new ConversationMessage(
            Guid.NewGuid().ToString("N"),
            threadId,
            role,
            authorId,
            content,
            DateTimeOffset.UtcNow,
            turnId,
            parentMessageId);
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

    private async Task<string> BuildCoordinatorReplyAsync(
        ConversationThread thread,
        CoordinationResult result,
        string userMessage,
        CancellationToken cancellationToken)
    {
        if (result.Rounds.Count == 0)
        {
            return "No specialists were available. Create an agent from the studio panel and I'll route future work through it.";
        }

        var recentConversation = thread.Messages
            .TakeLast(8)
            .Select(message => $"{ResolveParticipantName(thread, message.AuthorId)}: {message.Content}");

        var specialistUpdates = result.Rounds
            .SelectMany(round => round.Messages)
            .TakeLast(6)
            .Select(message => $"- {ResolveParticipantName(result.Thread, message.AuthorId)}: {message.Content}");

        var coordinatorRequest = new AgentExecutionRequest(
            Guid.NewGuid().ToString("N"),
            new AgentReference(CoordinatorId, "Coordinator", AgentKind.Coordinator),
            $$"""
            User request:
            {{userMessage}}

            Specialist updates:
            {{string.Join(Environment.NewLine, specialistUpdates)}}

            Write a direct assistant reply to the user. Be concise, conversational, and action-oriented. Do not expose internal ids or debug formatting.
            """,
            thread.ThreadId,
            Metadata: new Dictionary<string, string>
            {
                ["systemPrompt"] = "You are the coordinator. Reply as a real chat assistant who delegates work to specialists and explains outcomes in plain English.",
                ["conversationHistory"] = string.Join(Environment.NewLine, recentConversation)
            });

        var coordinatorResult = await _agentExecutor.ExecuteAsync(coordinatorRequest, cancellationToken);
        return string.IsNullOrWhiteSpace(coordinatorResult.Output)
            ? result.Summary ?? "The specialists completed their pass."
            : coordinatorResult.Output;
    }

    private static string ResolveParticipantName(ConversationThread thread, string authorId)
    {
        return thread.Participants.FirstOrDefault(participant => participant.ParticipantId == authorId)?.DisplayName
            ?? authorId;
    }
}
