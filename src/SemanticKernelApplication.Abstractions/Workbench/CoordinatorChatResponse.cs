using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Orchestration;

namespace SemanticKernelApplication.Abstractions.Workbench;

/// <summary>
/// Returns the updated conversation state after sending a coordinator message.
/// </summary>
/// <param name="ConversationId">Identifier of the active conversation.</param>
/// <param name="CoordinatorMessage">Coordinator reply shown to the user.</param>
/// <param name="Result">Underlying coordination result.</param>
/// <param name="Activity">Activity entries produced while handling the message.</param>
public sealed record CoordinatorChatResponse(
    Guid ConversationId,
    string CoordinatorMessage,
    CoordinationResult Result,
    IReadOnlyList<ActivityLogEntry> Activity);
