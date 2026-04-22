using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SemanticKernelApplication.Persistence.Configuration;
using SemanticKernelApplication.Persistence.Repositories;
using SemanticKernelApplication.Runtime.Services;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Persistence;

/// <summary>
/// Registers EF Core local persistence services and repository implementations.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalPersistenceOptions>(configuration.GetSection("LocalWorkbenchStore"));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<LocalPersistenceOptions>>().Value);
        services.AddPooledDbContextFactory<LocalWorkbenchDbContext>((serviceProvider, optionsBuilder) =>
        {
            var persistenceOptions = serviceProvider.GetRequiredService<LocalPersistenceOptions>();
            var databasePath = ResolveDatabasePath(persistenceOptions.DatabasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        });

        services.AddSingleton<ILocalWorkbenchDbContextProvider, PooledLocalWorkbenchDbContextProvider>();
        services.AddSingleton<ILocalWorkbenchConfigurationRepository, EfLocalWorkbenchConfigurationRepository>();
        services.AddSingleton<IConversationThreadRepository, EfConversationThreadRepository>();

        return services;
    }

    private static string ResolveDatabasePath(string databasePath)
    {
        var candidate = string.IsNullOrWhiteSpace(databasePath)
            ? ".appdata\\workbench.db"
            : databasePath.Trim();
        return Path.GetFullPath(candidate);
    }
}
