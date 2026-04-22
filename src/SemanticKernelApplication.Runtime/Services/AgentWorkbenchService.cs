using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Runtime.Services.Agents;
using SemanticKernelApplication.Runtime.Services.Workbench;
using SemanticKernelApplication.Tools.Providers;
using SemanticKernelApplication.Tools.Workspace;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class AgentWorkbenchService : IAgentWorkbenchService
{
    private readonly IAgentCreationService _agentCreationService;
    private readonly IWorkbenchSnapshotFactory _snapshotFactory;
    private readonly ICoordinatorChatService _coordinatorChatService;
    private readonly IActivitySink _activitySink;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly IProviderSessionConfiguration _providerSessionConfiguration;

    public AgentWorkbenchService(
        IAgentCreationService agentCreationService,
        IWorkbenchSnapshotFactory snapshotFactory,
        ICoordinatorChatService coordinatorChatService,
        IActivitySink activitySink,
        IWorkspaceContext workspaceContext,
        IProviderSessionConfiguration providerSessionConfiguration)
    {
        _agentCreationService = agentCreationService;
        _snapshotFactory = snapshotFactory;
        _coordinatorChatService = coordinatorChatService;
        _activitySink = activitySink;
        _workspaceContext = workspaceContext;
        _providerSessionConfiguration = providerSessionConfiguration;
    }

    public Task<WorkbenchSnapshot> GetSnapshotAsync(string? conversationId = null, CancellationToken cancellationToken = default) =>
        _snapshotFactory.GetWorkbenchSnapshotAsync(conversationId, cancellationToken);

    public Task<AgentDefinition> CreateAgentFromTextAsync(
        PlainTextAgentCreationRequest request,
        CancellationToken cancellationToken = default) =>
        _agentCreationService.CreateFromTextAsync(request, cancellationToken);

    public async Task<string> SetWorkspaceAsync(
        WorkspaceSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkspacePath);

        var workspacePath = _workspaceContext.SetRootPath(request.WorkspacePath);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                "workbench",
                ActivityLogEntry.Builder
                    .WithSequence(0)
                    .WithKind(ActivityKind.Status)
                    .WithStatus(ActivityStatus.Completed)
                    .WithSeverity(ActivitySeverity.Information)
                    .WithTitle("Workspace updated")
                    .WithMessage($"Agent tool execution is now constrained to {workspacePath}.")
                    .WithTimestampUtc(DateTimeOffset.UtcNow)
                    .WithMetadata(new Dictionary<string, string> { ["workspacePath"] = workspacePath })
                    .Build()),
            cancellationToken);

        return workspacePath;
    }

    public async Task<GlobalModelConfiguration> SetGlobalModelConfigurationAsync(
        GlobalModelConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        var configuration = _providerSessionConfiguration.Update(request);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                "workbench",
                ActivityLogEntry.Builder
                    .WithSequence(0)
                    .WithKind(ActivityKind.Status)
                    .WithStatus(ActivityStatus.Completed)
                    .WithSeverity(ActivitySeverity.Information)
                    .WithTitle("Global model updated")
                    .WithMessage($"The coordinator and all agents will now use {configuration.SelectedProviderId} / {configuration.SelectedModelId}.")
                    .WithTimestampUtc(DateTimeOffset.UtcNow)
                    .WithMetadata(new Dictionary<string, string>
                    {
                        ["providerId"] = configuration.SelectedProviderId,
                        ["modelId"] = configuration.SelectedModelId,
                        ["apiKeyConfigured"] = configuration.ApiKeyConfigured.ToString()
                    })
                    .Build()),
            cancellationToken);

        return configuration;
    }

    public Task<CoordinatorChatResponse> SendCoordinatorMessageAsync(
        CoordinatorChatRequest request,
        CancellationToken cancellationToken = default) =>
        _coordinatorChatService.SendAsync(request, cancellationToken);
}
