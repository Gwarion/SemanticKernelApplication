using System.Text.Json.Serialization;
using SemanticKernelApplication.Abstractions.Conversations;

namespace SemanticKernelApplication.Abstractions.Orchestration;

/// <summary>
/// Represents one round of coordinator-to-agent interaction.
/// </summary>
public sealed class CoordinationRound
{
    [JsonConstructor]
    public CoordinationRound(int roundNumber, IReadOnlyList<ConversationMessage> messages)
    {
        RoundNumber = roundNumber;
        Messages = messages;
    }

    public int RoundNumber { get; }
    public IReadOnlyList<ConversationMessage> Messages { get; }
}
