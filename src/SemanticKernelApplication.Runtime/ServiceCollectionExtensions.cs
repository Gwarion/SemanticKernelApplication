using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Orchestration;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Runtime.Services;

namespace SemanticKernelApplication.Runtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentWorkbenchRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InMemoryActivityLog>();
        services.AddSingleton<IActivitySink>(provider => provider.GetRequiredService<InMemoryActivityLog>());
        services.AddSingleton<IActivityStreamReader>(provider => provider.GetRequiredService<InMemoryActivityLog>());

        services.AddSingleton<IAgentDefinitionStore, InMemoryAgentDefinitionStore>();
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        services.AddSingleton<PlainTextAgentDefinitionFactory>();
        services.AddSingleton<IAgentExecutor, SemanticKernelAgentExecutor>();
        services.AddSingleton<ICoordinatorOrchestrator, SequentialCoordinatorOrchestrator>();
        services.AddSingleton<IAgentWorkbenchService, AgentWorkbenchService>();

        return services;
    }
}
