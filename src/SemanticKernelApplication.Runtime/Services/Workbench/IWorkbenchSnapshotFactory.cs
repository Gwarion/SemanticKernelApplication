using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Runtime.Services.Workbench;

public interface IWorkbenchSnapshotFactory
{
    Task<WorkbenchSnapshot> GetWorkbenchSnapshotAsync(string? conversationId = null, CancellationToken cancellationToken = default);

    void SetActiveConversation(string? conversationId);
}
