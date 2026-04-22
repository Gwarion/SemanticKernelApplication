namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Stores prompts, goals, and constraints used to guide an agent.
/// </summary>
public sealed class AgentInstructionSet
{
    [System.Text.Json.Serialization.JsonConstructor]
    private AgentInstructionSet(
        string systemPrompt,
        IReadOnlyList<string>? goals,
        IReadOnlyList<string>? constraints,
        IReadOnlyDictionary<string, string>? variables)
    {
        SystemPrompt = systemPrompt;
        Goals = goals;
        Constraints = constraints;
        Variables = variables;
    }

    /// <summary>
    /// Gets a new builder for creating an <see cref="AgentInstructionSet"/>.
    /// </summary>
    public static AgentInstructionSetBuilder Builder => new();

    /// <summary>
    /// Represents an empty instruction set.
    /// </summary>
    public static AgentInstructionSet Empty { get; } = Builder.WithSystemPrompt(string.Empty).Build();

    /// <summary>
    /// Gets the base system prompt applied to the agent.
    /// </summary>
    public string SystemPrompt { get; }

    /// <summary>
    /// Gets the optional list of goals the agent should pursue.
    /// </summary>
    public IReadOnlyList<string>? Goals { get; }

    /// <summary>
    /// Gets the optional list of constraints the agent should obey.
    /// </summary>
    public IReadOnlyList<string>? Constraints { get; }

    /// <summary>
    /// Gets the optional template variables available to the prompt.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Variables { get; }

    /// <summary>
    /// Creates a builder pre-populated with this instance's values.
    /// </summary>
    public AgentInstructionSetBuilder ToBuilder() =>
        Builder
            .WithSystemPrompt(SystemPrompt)
            .WithGoals(Goals)
            .WithConstraints(Constraints)
            .WithVariables(Variables);

    /// <summary>
    /// Builds <see cref="AgentInstructionSet"/> instances.
    /// </summary>
    public sealed class AgentInstructionSetBuilder
    {
        private string? _systemPrompt;
        private IReadOnlyList<string>? _goals;
        private IReadOnlyList<string>? _constraints;
        private IReadOnlyDictionary<string, string>? _variables;

        public AgentInstructionSetBuilder WithSystemPrompt(string systemPrompt)
        {
            _systemPrompt = systemPrompt;
            return this;
        }

        public AgentInstructionSetBuilder WithGoals(IReadOnlyList<string>? goals)
        {
            _goals = goals;
            return this;
        }

        public AgentInstructionSetBuilder WithConstraints(IReadOnlyList<string>? constraints)
        {
            _constraints = constraints;
            return this;
        }

        public AgentInstructionSetBuilder WithVariables(IReadOnlyDictionary<string, string>? variables)
        {
            _variables = variables;
            return this;
        }

        public AgentInstructionSet Build()
        {
            if (_systemPrompt is null)
            {
                throw new InvalidOperationException("System prompt is required.");
            }

            return new AgentInstructionSet(_systemPrompt, _goals, _constraints, _variables);
        }
    }
}
