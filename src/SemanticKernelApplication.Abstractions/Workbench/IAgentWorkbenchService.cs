using SemanticKernelApplication.Abstractions.Agents;

namespace SemanticKernelApplication.Abstractions.Workbench;

/// <summary>
/// Exposes the main application use cases consumed by the workbench UI.
/// </summary>
public interface IAgentWorkbenchService
{
    /// <summary>
    /// Gets the current workbench snapshot for the requested conversation.
    /// </summary>
    /// <param name="conversationId">Optional conversation identifier to load.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<WorkbenchSnapshot> GetSnapshotAsync(string? conversationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new agent from a natural-language description.
    /// </summary>
    /// <param name="request">Creation request describing the desired agent.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<AgentDefinition> CreateAgentFromTextAsync(
        PlainTextAgentCreationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active workspace for the current session.
    /// </summary>
    /// <param name="request">Workspace selection request.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<string> SetWorkspaceAsync(
        WorkspaceSelectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the selected provider, model, and optional API key.
    /// </summary>
    /// <param name="request">Requested configuration update.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<GlobalModelConfiguration> SetGlobalModelConfigurationAsync(
        GlobalModelConfigurationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a user message to the coordinator and returns the updated thread state.
    /// </summary>
    /// <param name="request">Chat request to process.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<CoordinatorChatResponse> SendCoordinatorMessageAsync(
        CoordinatorChatRequest request,
        CancellationToken cancellationToken = default);
}
