using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemanticKernelApplication.Tools.Configuration;
using SemanticKernelApplication.Tools.Kernel;
using SemanticKernelApplication.Tools.Plugins;
using SemanticKernelApplication.Tools.Providers;
using SemanticKernelApplication.Tools.Workspace;

namespace SemanticKernelApplication.Tools;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkspaceTools(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WorkspaceToolOptions>(configuration.GetSection("WorkspaceTools"));
        services.Configure<LocalWorkbenchStoreOptions>(configuration.GetSection("LocalWorkbenchStore"));

        services.AddSingleton<FileSystemPlugin>();
        services.AddSingleton<ShellPlugin>();
        services.AddSingleton<GitPlugin>();
        services.AddSingleton<ILocalWorkbenchConfigurationStore, LocalWorkbenchConfigurationStore>();
        services.AddSingleton<IWorkspaceContext, WorkspaceContext>();
        services.AddSingleton<IProviderSessionConfiguration, ProviderSessionConfiguration>();
        services.AddSingleton<IWorkspacePluginCatalog, WorkspacePluginCatalog>();
        services.AddSingleton<IAiProviderCatalog, ConfigurationAiProviderCatalog>();

        return services;
    }
}
