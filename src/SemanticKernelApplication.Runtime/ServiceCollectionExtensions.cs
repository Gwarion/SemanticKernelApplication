using Microsoft.Extensions.DependencyInjection;
using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Orchestration;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Runtime.Services;
using SemanticKernelApplication.Runtime.Services.Agents;
using SemanticKernelApplication.Runtime.Services.Workbench;

namespace SemanticKernelApplication.Runtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentWorkbenchRuntime(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryActivityLog>();
        services.AddSingleton<IActivitySink>(provider => provider.GetRequiredService<InMemoryActivityLog>());
        services.AddSingleton<IActivityStreamReader>(provider => provider.GetRequiredService<InMemoryActivityLog>());

        services.AddSingleton<IAgentDefinitionStore, InMemoryAgentDefinitionStore>();
        services.AddSingleton<IConversationStore, LocalConversationStore>();
        services.AddSingleton<PlainTextAgentDefinitionFactory>();
        services.AddSingleton<IAgentCreationService, AgentCreationService>();
        services.AddScoped<IConversationSessionAccessor, ConversationSessionAccessor>();
        services.AddScoped<IWorkbenchSnapshotFactory, WorkbenchSnapshotFactory>();
        services.AddScoped<ICoordinatorChatService, CoordinatorChatService>();
        services.AddSingleton<IAgentExecutor, SemanticKernelAgentExecutor>();
        services.AddSingleton<ICoordinatorOrchestrator, SequentialCoordinatorOrchestrator>();
        services.AddScoped<IAgentWorkbenchService, AgentWorkbenchService>();

        return services;
    }
}
