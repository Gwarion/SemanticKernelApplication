using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Orchestration;
using SemanticKernelApplication.Abstractions.Providers;

namespace SemanticKernelApplication.Abstractions.Workbench;

public sealed record PlainTextAgentCreationRequest(
    string Description,
    string? PreferredProviderId = null);

public sealed record CoordinatorChatRequest(
    string Message,
    string? ConversationId = null);

public sealed record WorkspaceSelectionRequest(string WorkspacePath);

public sealed record GlobalModelConfiguration(
    string SelectedProviderId,
    string SelectedModelId,
    bool ApiKeyConfigured,
    string? ApiKey = null);

public sealed record GlobalModelConfigurationRequest(
    string SelectedProviderId,
    string SelectedModelId,
    string? ApiKey = null);

public sealed record CoordinatorChatResponse(
    string ConversationId,
    string CoordinatorMessage,
    CoordinationResult Result,
    IReadOnlyList<ActivityLogEntry> Activity);

public sealed record WorkbenchSnapshot(
    IReadOnlyList<AgentDefinition> Agents,
    IReadOnlyList<ModelProviderDefinition> Providers,
    GlobalModelConfiguration ModelConfiguration,
    string WorkspacePath,
    IReadOnlyList<string> KnownWorkspacePaths,
    ConversationThread? ActiveConversation,
    IReadOnlyList<ActivityLogEntry> RecentActivity);

public interface IAgentWorkbenchService
{
    Task<WorkbenchSnapshot> GetSnapshotAsync(string? conversationId = null, CancellationToken cancellationToken = default);

    Task<AgentDefinition> CreateAgentFromTextAsync(
        PlainTextAgentCreationRequest request,
        CancellationToken cancellationToken = default);

    Task<string> SetWorkspaceAsync(
        WorkspaceSelectionRequest request,
        CancellationToken cancellationToken = default);

    Task<GlobalModelConfiguration> SetGlobalModelConfigurationAsync(
        GlobalModelConfigurationRequest request,
        CancellationToken cancellationToken = default);

    Task<CoordinatorChatResponse> SendCoordinatorMessageAsync(
        CoordinatorChatRequest request,
        CancellationToken cancellationToken = default);
}
