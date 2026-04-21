using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Runtime.Services.Workbench;

public interface IWorkbenchSnapshotFactory
{
    Task<WorkbenchSnapshot> CreateAsync(CancellationToken cancellationToken = default);

    void SetActiveConversation(string? conversationId);
}
