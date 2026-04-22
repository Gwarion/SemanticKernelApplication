namespace SemanticKernelApplication.Abstractions.Conversations;

/// <summary>
/// Describes one participant in a conversation thread.
/// </summary>
/// <param name="ParticipantId">Unique identifier for the participant.</param>
/// <param name="DisplayName">Name shown in the UI.</param>
/// <param name="Kind">Broad participant category.</param>
/// <param name="AgentId">Optional backing agent identifier.</param>
/// <param name="Metadata">Optional contextual metadata.</param>
public sealed record ConversationParticipant(
    string ParticipantId,
    string DisplayName,
    ConversationParticipantKind Kind,
    string? AgentId = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
