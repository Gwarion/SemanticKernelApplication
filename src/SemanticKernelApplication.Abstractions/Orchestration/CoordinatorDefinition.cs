using System.Text.Json.Serialization;
using SemanticKernelApplication.Abstractions.Agents;

namespace SemanticKernelApplication.Abstractions.Orchestration;

/// <summary>
/// Describes the coordinator agent and the policy it should use.
/// </summary>
public sealed class CoordinatorDefinition
{
    [JsonConstructor]
    private CoordinatorDefinition(
        string id,
        string name,
        string description,
        CoordinationPolicy policy,
        AgentInstructionSet instructions,
        IReadOnlyDictionary<string, string>? metadata)
    {
        Id = id;
        Name = name;
        Description = description;
        Policy = policy;
        Instructions = instructions;
        Metadata = metadata;
    }

    public static CoordinatorDefinitionBuilder Builder => new();

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public CoordinationPolicy Policy { get; }
    public AgentInstructionSet Instructions { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public sealed class CoordinatorDefinitionBuilder
    {
        private string? _id;
        private string? _name;
        private string? _description;
        private CoordinationPolicy? _policy;
        private AgentInstructionSet? _instructions;
        private IReadOnlyDictionary<string, string>? _metadata;

        public CoordinatorDefinitionBuilder WithId(string id) { _id = id; return this; }
        public CoordinatorDefinitionBuilder WithName(string name) { _name = name; return this; }
        public CoordinatorDefinitionBuilder WithDescription(string description) { _description = description; return this; }
        public CoordinatorDefinitionBuilder WithPolicy(CoordinationPolicy policy) { _policy = policy; return this; }
        public CoordinatorDefinitionBuilder WithInstructions(AgentInstructionSet instructions) { _instructions = instructions; return this; }
        public CoordinatorDefinitionBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata) { _metadata = metadata; return this; }

        public CoordinatorDefinition Build()
        {
            if (string.IsNullOrWhiteSpace(_id)) throw new InvalidOperationException("Coordinator id is required.");
            if (string.IsNullOrWhiteSpace(_name)) throw new InvalidOperationException("Coordinator name is required.");
            if (string.IsNullOrWhiteSpace(_description)) throw new InvalidOperationException("Coordinator description is required.");
            if (_policy is null) throw new InvalidOperationException("Coordination policy is required.");
            if (_instructions is null) throw new InvalidOperationException("Coordinator instructions are required.");

            return new CoordinatorDefinition(_id, _name, _description, _policy, _instructions, _metadata);
        }
    }
}
