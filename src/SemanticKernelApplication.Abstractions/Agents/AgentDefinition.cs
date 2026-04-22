using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Represents a fully materialized agent definition stored in the workbench.
/// </summary>
public sealed class AgentDefinition
{
    [JsonConstructor]
    private AgentDefinition(
        Guid id,
        string name,
        AgentKind kind,
        string description,
        AgentInstructionSet instructions,
        string? providerId,
        IReadOnlyList<AgentCapability>? capabilities,
        IReadOnlyCollection<string>? tags,
        IReadOnlyDictionary<string, string>? metadata,
        string? version,
        bool isEnabled,
        DateTimeOffset? createdAtUtc,
        DateTimeOffset? updatedAtUtc)
    {
        Id = id;
        Name = name;
        Kind = kind;
        Description = description;
        Instructions = instructions;
        ProviderId = providerId;
        Capabilities = capabilities;
        Tags = tags;
        Metadata = metadata;
        Version = version;
        IsEnabled = isEnabled;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        CapabilitiesOrEmpty = new ReadOnlyCollection<AgentCapability>((Capabilities ?? []).ToArray());
        TagsOrEmpty = new ReadOnlyCollection<string>((Tags ?? []).ToArray());
        CreatedAtUtcOrNow = CreatedAtUtc ?? DateTimeOffset.UtcNow;
        UpdatedAtUtcOrNow = UpdatedAtUtc ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets a new builder for creating an <see cref="AgentDefinition"/>.
    /// </summary>
    public static AgentDefinitionBuilder Builder => new();

    public Guid Id { get; }
    public string Name { get; }
    public AgentKind Kind { get; }
    public string Description { get; }
    public AgentInstructionSet Instructions { get; }
    public string? ProviderId { get; }
    public IReadOnlyList<AgentCapability>? Capabilities { get; }
    public IReadOnlyCollection<string>? Tags { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }
    public string? Version { get; }
    public bool IsEnabled { get; }
    public DateTimeOffset? CreatedAtUtc { get; }
    public DateTimeOffset? UpdatedAtUtc { get; }

    /// <summary>
    /// Returns capabilities as a non-null read-only collection.
    /// </summary>
    public IReadOnlyList<AgentCapability> CapabilitiesOrEmpty { get; }

    /// <summary>
    /// Returns tags as a non-null read-only collection.
    /// </summary>
    public IReadOnlyCollection<string> TagsOrEmpty { get; }

    /// <summary>
    /// Returns the creation timestamp, falling back to the current time when unset.
    /// </summary>
    public DateTimeOffset CreatedAtUtcOrNow { get; }

    /// <summary>
    /// Returns the last update timestamp, falling back to the current time when unset.
    /// </summary>
    public DateTimeOffset UpdatedAtUtcOrNow { get; }

    /// <summary>
    /// Creates a builder pre-populated with this instance's values.
    /// </summary>
    public AgentDefinitionBuilder ToBuilder() =>
        Builder
            .WithId(Id)
            .WithName(Name)
            .WithKind(Kind)
            .WithDescription(Description)
            .WithInstructions(Instructions)
            .WithProviderId(ProviderId)
            .WithCapabilities(Capabilities)
            .WithTags(Tags)
            .WithMetadata(Metadata)
            .WithVersion(Version)
            .WithEnabled(IsEnabled)
            .WithCreatedAtUtc(CreatedAtUtc)
            .WithUpdatedAtUtc(UpdatedAtUtc);

    /// <summary>
    /// Builds <see cref="AgentDefinition"/> instances.
    /// </summary>
    public sealed class AgentDefinitionBuilder
    {
        private Guid? _id;
        private string? _name;
        private AgentKind? _kind;
        private string? _description;
        private AgentInstructionSet? _instructions;
        private string? _providerId;
        private IReadOnlyList<AgentCapability>? _capabilities;
        private IReadOnlyCollection<string>? _tags;
        private IReadOnlyDictionary<string, string>? _metadata;
        private string? _version;
        private bool _isEnabled = true;
        private DateTimeOffset? _createdAtUtc;
        private DateTimeOffset? _updatedAtUtc;

        public AgentDefinitionBuilder WithId(Guid id) { _id = id; return this; }
        public AgentDefinitionBuilder WithName(string name) { _name = name; return this; }
        public AgentDefinitionBuilder WithKind(AgentKind kind) { _kind = kind; return this; }
        public AgentDefinitionBuilder WithDescription(string description) { _description = description; return this; }
        public AgentDefinitionBuilder WithInstructions(AgentInstructionSet instructions) { _instructions = instructions; return this; }
        public AgentDefinitionBuilder WithProviderId(string? providerId) { _providerId = providerId; return this; }
        public AgentDefinitionBuilder WithCapabilities(IReadOnlyList<AgentCapability>? capabilities) { _capabilities = capabilities; return this; }
        public AgentDefinitionBuilder WithTags(IReadOnlyCollection<string>? tags) { _tags = tags; return this; }
        public AgentDefinitionBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata) { _metadata = metadata; return this; }
        public AgentDefinitionBuilder WithVersion(string? version) { _version = version; return this; }
        public AgentDefinitionBuilder WithEnabled(bool isEnabled) { _isEnabled = isEnabled; return this; }
        public AgentDefinitionBuilder WithCreatedAtUtc(DateTimeOffset? createdAtUtc) { _createdAtUtc = createdAtUtc; return this; }
        public AgentDefinitionBuilder WithUpdatedAtUtc(DateTimeOffset? updatedAtUtc) { _updatedAtUtc = updatedAtUtc; return this; }

        public AgentDefinition Build()
        {
            if (_id is null || _id == Guid.Empty) throw new InvalidOperationException("Agent id is required.");
            ArgumentException.ThrowIfNullOrWhiteSpace(_name);
            if (_kind is null) throw new InvalidOperationException("Agent kind is required.");
            ArgumentException.ThrowIfNullOrWhiteSpace(_description);
            ArgumentNullException.ThrowIfNull(_instructions);

            return new AgentDefinition(
                _id.Value,
                _name,
                _kind.Value,
                _description,
                _instructions,
                _providerId,
                _capabilities,
                _tags,
                _metadata,
                _version,
                _isEnabled,
                _createdAtUtc,
                _updatedAtUtc);
        }
    }
}
