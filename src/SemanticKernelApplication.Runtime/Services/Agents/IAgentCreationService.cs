using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Runtime.Services.Agents;

public interface IAgentCreationService
{
    Task<AgentDefinition> CreateFromTextAsync(
        PlainTextAgentCreationRequest request,
        CancellationToken cancellationToken = default);
}
