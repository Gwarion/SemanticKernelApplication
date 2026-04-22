using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Providers;

namespace SemanticKernelApplication.Abstractions.Workbench;

/// <summary>
/// Captures the current state required to render the workbench UI.
/// </summary>
/// <param name="Agents">Agents currently registered in the workbench.</param>
/// <param name="Providers">Available model providers.</param>
/// <param name="ModelConfiguration">Current persisted provider/model defaults.</param>
/// <param name="WorkspacePath">Current active workspace path.</param>
/// <param name="KnownWorkspacePaths">Saved workspace suggestions.</param>
/// <param name="ActiveConversation">Conversation currently displayed in the UI.</param>
/// <param name="RecentActivity">Recent activity entries for diagnostics and streaming.</param>
public sealed record WorkbenchSnapshot(
    IReadOnlyList<AgentDefinition> Agents,
    IReadOnlyList<ModelProviderDefinition> Providers,
    GlobalModelConfiguration ModelConfiguration,
    string WorkspacePath,
    IReadOnlyList<string> KnownWorkspacePaths,
    ConversationThread? ActiveConversation,
    IReadOnlyList<ActivityLogEntry> RecentActivity);
