using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Runtime.Services.Agents;
using SemanticKernelApplication.Runtime.Services.Workbench;
using SemanticKernelApplication.Tools.Configuration;
using SemanticKernelApplication.Tools.Providers;
using SemanticKernelApplication.Tools.Workspace;
using Microsoft.Extensions.Options;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class AgentWorkbenchService : IAgentWorkbenchService
{
    private readonly IAgentCreationService _agentCreationService;
    private readonly IWorkbenchSnapshotFactory _snapshotFactory;
    private readonly ICoordinatorChatService _coordinatorChatService;
    private readonly IActivitySink _activitySink;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly IProviderSessionConfiguration _providerSessionConfiguration;
    private readonly AgentProviderOptions _providerOptions;

    public AgentWorkbenchService(
        IAgentCreationService agentCreationService,
        IWorkbenchSnapshotFactory snapshotFactory,
        ICoordinatorChatService coordinatorChatService,
        IActivitySink activitySink,
        IWorkspaceContext workspaceContext,
        IProviderSessionConfiguration providerSessionConfiguration,
        IOptions<AgentProviderOptions> providerOptions)
    {
        _agentCreationService = agentCreationService;
        _snapshotFactory = snapshotFactory;
        _coordinatorChatService = coordinatorChatService;
        _activitySink = activitySink;
        _workspaceContext = workspaceContext;
        _providerSessionConfiguration = providerSessionConfiguration;
        _providerOptions = providerOptions.Value;
    }

    public Task<WorkbenchSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        _snapshotFactory.CreateAsync(cancellationToken);

    public Task<AgentDefinition> CreateAgentFromTextAsync(
        PlainTextAgentCreationRequest request,
        CancellationToken cancellationToken = default) =>
        _agentCreationService.CreateFromTextAsync(request, cancellationToken);

    public async Task<string> SetWorkspaceAsync(
        WorkspaceSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.WorkspacePath))
        {
            throw new ArgumentException("Workspace path is required.", nameof(request));
        }

        var workspacePath = _workspaceContext.SetRootPath(request.WorkspacePath);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                "workbench",
                new ActivityLogEntry(
                    0,
                    ActivityKind.Status,
                    ActivityStatus.Completed,
                    ActivitySeverity.Information,
                    "Workspace updated",
                    $"Agent tool execution is now constrained to {workspacePath}.",
                    DateTimeOffset.UtcNow,
                    Metadata: new Dictionary<string, string> { ["workspacePath"] = workspacePath })),
            cancellationToken);

        return workspacePath;
    }

    public async Task<GlobalModelConfiguration> SetGlobalModelConfigurationAsync(
        GlobalModelConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        var configuration = _providerSessionConfiguration.Update(request, _providerOptions.Providers);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                "workbench",
                new ActivityLogEntry(
                    0,
                    ActivityKind.Status,
                    ActivityStatus.Completed,
                    ActivitySeverity.Information,
                    "Global model updated",
                    $"The coordinator and all agents will now use {configuration.SelectedProviderId}.",
                    DateTimeOffset.UtcNow,
                    Metadata: new Dictionary<string, string>
                    {
                        ["providerId"] = configuration.SelectedProviderId,
                        ["apiKeyConfigured"] = configuration.ApiKeyConfigured.ToString()
                    })),
            cancellationToken);

        return configuration;
    }

    public Task<CoordinatorChatResponse> SendCoordinatorMessageAsync(
        CoordinatorChatRequest request,
        CancellationToken cancellationToken = default) =>
        _coordinatorChatService.SendAsync(request, cancellationToken);
}
