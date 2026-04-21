using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Configuration;
using SemanticKernelApplication.Tools.Providers;
using SemanticKernelApplication.Tools.Workspace;
using Microsoft.Extensions.Options;

namespace SemanticKernelApplication.Runtime.Services.Workbench;

public sealed class WorkbenchSnapshotFactory : IWorkbenchSnapshotFactory
{
    private readonly IAgentDefinitionStore _agentDefinitionStore;
    private readonly IConversationStore _conversationStore;
    private readonly IAiProviderCatalog _providerCatalog;
    private readonly InMemoryActivityLog _activityLog;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly IProviderSessionConfiguration _providerSessionConfiguration;
    private readonly AgentProviderOptions _providerOptions;
    private string? _activeConversationId;

    public WorkbenchSnapshotFactory(
        IAgentDefinitionStore agentDefinitionStore,
        IConversationStore conversationStore,
        IAiProviderCatalog providerCatalog,
        InMemoryActivityLog activityLog,
        IWorkspaceContext workspaceContext,
        IProviderSessionConfiguration providerSessionConfiguration,
        IOptions<AgentProviderOptions> providerOptions)
    {
        _agentDefinitionStore = agentDefinitionStore;
        _conversationStore = conversationStore;
        _providerCatalog = providerCatalog;
        _activityLog = activityLog;
        _workspaceContext = workspaceContext;
        _providerSessionConfiguration = providerSessionConfiguration;
        _providerOptions = providerOptions.Value;
    }

    public async Task<WorkbenchSnapshot> CreateAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _agentDefinitionStore.ListAsync(cancellationToken);
        var conversation = _activeConversationId is null
            ? null
            : await _conversationStore.GetAsync(_activeConversationId, cancellationToken);

        return new WorkbenchSnapshot(
            agents,
            _providerCatalog.GetProviders(),
            _providerSessionConfiguration.GetConfiguration(_providerOptions.Providers),
            _workspaceContext.CurrentRootPath,
            conversation,
            _activityLog.GetRecent(80));
    }

    public void SetActiveConversation(string? conversationId)
    {
        _activeConversationId = conversationId;
    }
}
