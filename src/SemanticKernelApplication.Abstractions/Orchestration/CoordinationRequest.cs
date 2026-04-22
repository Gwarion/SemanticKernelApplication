using System.Text.Json.Serialization;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;

namespace SemanticKernelApplication.Abstractions.Orchestration;

/// <summary>
/// Carries the full input required to run a coordination workflow.
/// </summary>
public sealed class CoordinationRequest
{
    [JsonConstructor]
    private CoordinationRequest(
        Guid operationId,
        CoordinatorDefinition coordinator,
        ConversationThread thread,
        IReadOnlyList<AgentReference> agents,
        string objective,
        IReadOnlyDictionary<string, string>? metadata,
        DateTimeOffset? requestedAtUtc)
    {
        OperationId = operationId;
        Coordinator = coordinator;
        Thread = thread;
        Agents = agents;
        Objective = objective;
        Metadata = metadata;
        RequestedAtUtc = requestedAtUtc;
    }

    public static CoordinationRequestBuilder Builder => new();

    public Guid OperationId { get; }
    public CoordinatorDefinition Coordinator { get; }
    public ConversationThread Thread { get; }
    public IReadOnlyList<AgentReference> Agents { get; }
    public string Objective { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }
    public DateTimeOffset? RequestedAtUtc { get; }

    public sealed class CoordinationRequestBuilder
    {
        private Guid? _operationId;
        private CoordinatorDefinition? _coordinator;
        private ConversationThread? _thread;
        private IReadOnlyList<AgentReference>? _agents;
        private string? _objective;
        private IReadOnlyDictionary<string, string>? _metadata;
        private DateTimeOffset? _requestedAtUtc;

        public CoordinationRequestBuilder WithOperationId(Guid operationId) { _operationId = operationId; return this; }
        public CoordinationRequestBuilder WithCoordinator(CoordinatorDefinition coordinator) { _coordinator = coordinator; return this; }
        public CoordinationRequestBuilder WithThread(ConversationThread thread) { _thread = thread; return this; }
        public CoordinationRequestBuilder WithAgents(IReadOnlyList<AgentReference> agents) { _agents = agents; return this; }
        public CoordinationRequestBuilder WithObjective(string objective) { _objective = objective; return this; }
        public CoordinationRequestBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata) { _metadata = metadata; return this; }
        public CoordinationRequestBuilder WithRequestedAtUtc(DateTimeOffset? requestedAtUtc) { _requestedAtUtc = requestedAtUtc; return this; }

        public CoordinationRequest Build()
        {
            if (_operationId is null || _operationId == Guid.Empty) throw new InvalidOperationException("Operation id is required.");
            if (_coordinator is null) throw new InvalidOperationException("Coordinator is required.");
            if (_thread is null) throw new InvalidOperationException("Conversation thread is required.");
            if (_agents is null) throw new InvalidOperationException("Agent list is required.");
            if (string.IsNullOrWhiteSpace(_objective)) throw new InvalidOperationException("Objective is required.");

            return new CoordinationRequest(_operationId.Value, _coordinator, _thread, _agents, _objective, _metadata, _requestedAtUtc);
        }
    }
}
