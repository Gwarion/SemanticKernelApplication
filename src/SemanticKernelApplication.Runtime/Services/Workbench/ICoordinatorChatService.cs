using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Runtime.Services.Workbench;

public interface ICoordinatorChatService
{
    Task<CoordinatorChatResponse> SendAsync(
        CoordinatorChatRequest request,
        CancellationToken cancellationToken = default);
}
