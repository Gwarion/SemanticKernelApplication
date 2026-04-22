using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Providers;
using SemanticKernelApplication.Tools.Workspace;

namespace SemanticKernelApplication.Runtime.Services.Workbench;

public sealed class WorkbenchSnapshotFactory : IWorkbenchSnapshotFactory
{
    private readonly IAgentDefinitionStore _agentDefinitionStore;
    private readonly IConversationStore _conversationStore;
    private readonly IAiProviderCatalog _providerCatalog;
    private readonly InMemoryActivityLog _activityLog;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly IProviderSessionConfiguration _providerSessionConfiguration;
    private string? _activeConversationId;

    public WorkbenchSnapshotFactory(
        IAgentDefinitionStore agentDefinitionStore,
        IConversationStore conversationStore,
        IAiProviderCatalog providerCatalog,
        InMemoryActivityLog activityLog,
        IWorkspaceContext workspaceContext,
        IProviderSessionConfiguration providerSessionConfiguration)
    {
        _agentDefinitionStore = agentDefinitionStore;
        _conversationStore = conversationStore;
        _providerCatalog = providerCatalog;
        _activityLog = activityLog;
        _workspaceContext = workspaceContext;
        _providerSessionConfiguration = providerSessionConfiguration;
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
            _providerSessionConfiguration.GetConfiguration(),
            _workspaceContext.CurrentRootPath,
            conversation,
            _activityLog.GetRecent(80));
    }

    public void SetActiveConversation(string? conversationId)
    {
        _activeConversationId = conversationId;
    }
}
