using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Configuration;
using SemanticKernelApplication.Tools.Providers;
using SemanticKernelApplication.Tools.Workspace;

namespace SemanticKernelApplication.Runtime.Services.Workbench;

public sealed class WorkbenchSnapshotFactory : IWorkbenchSnapshotFactory
{
    private readonly IAgentDefinitionStore _agentDefinitionStore;
    private readonly IConversationStore _conversationStore;
    private readonly IAiProviderCatalog _providerCatalog;
    private readonly ILocalWorkbenchConfigurationStore _configurationStore;
    private readonly InMemoryActivityLog _activityLog;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly IProviderSessionConfiguration _providerSessionConfiguration;
    private readonly IConversationSessionAccessor _conversationSessionAccessor;

    public WorkbenchSnapshotFactory(
        IAgentDefinitionStore agentDefinitionStore,
        IConversationStore conversationStore,
        IAiProviderCatalog providerCatalog,
        ILocalWorkbenchConfigurationStore configurationStore,
        InMemoryActivityLog activityLog,
        IWorkspaceContext workspaceContext,
        IProviderSessionConfiguration providerSessionConfiguration,
        IConversationSessionAccessor conversationSessionAccessor)
    {
        _agentDefinitionStore = agentDefinitionStore;
        _conversationStore = conversationStore;
        _providerCatalog = providerCatalog;
        _configurationStore = configurationStore;
        _activityLog = activityLog;
        _workspaceContext = workspaceContext;
        _providerSessionConfiguration = providerSessionConfiguration;
        _conversationSessionAccessor = conversationSessionAccessor;
    }

    public async Task<WorkbenchSnapshot> GetWorkbenchSnapshotAsync(string? conversationId = null, CancellationToken cancellationToken = default)
    {
        var agents = await _agentDefinitionStore.ListAsync(cancellationToken);
        var effectiveConversationId = TryParseConversationId(conversationId) ?? _conversationSessionAccessor.ActiveConversationId;
        var conversation = effectiveConversationId is null
            ? null
            : await _conversationStore.GetAsync(effectiveConversationId.Value, cancellationToken);

        return new WorkbenchSnapshot(
            agents,
            _providerCatalog.GetProviders(),
            _providerSessionConfiguration.GetConfiguration(),
            _workspaceContext.CurrentRootPath,
            _configurationStore.GetKnownWorkspacePaths(),
            conversation,
            _activityLog.GetRecent(80));
    }

    public void SetActiveConversation(Guid? conversationId)
    {
        _conversationSessionAccessor.ActiveConversationId = conversationId;
    }

    private static Guid? TryParseConversationId(string? conversationId) =>
        Guid.TryParse(conversationId, out var parsedConversationId)
            ? parsedConversationId
            : null;
}
