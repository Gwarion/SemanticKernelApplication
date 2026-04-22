using Microsoft.EntityFrameworkCore;
using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Persistence.Entities;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Persistence.Repositories;

internal sealed class EfLocalWorkbenchConfigurationRepository(ILocalWorkbenchDbContextProvider dbContextProvider) : ILocalWorkbenchConfigurationRepository
{
    private const string SingletonId = "main";

    public LocalWorkbenchConfigurationRecord Load(string defaultWorkspacePath)
    {
        using var context = dbContextProvider.CreateDbContext();
        SyncCatalog(context);
        return BuildRecord(context, defaultWorkspacePath);
    }

    public LocalWorkbenchConfigurationRecord SaveWorkspace(LocalWorkbenchConfigurationRecord current, string workspacePath)
    {
        using var context = dbContextProvider.CreateDbContext();
        UpsertKnownWorkspace(context, workspacePath);

        var configuration = context.WorkbenchConfigurations.Single(item => item.ConfigurationId == SingletonId);
        configuration.WorkspacePath = workspacePath;
        context.SaveChanges();

        return BuildRecord(context, workspacePath);
    }

    public LocalWorkbenchConfigurationRecord SaveGlobalModelConfiguration(
        LocalWorkbenchConfigurationRecord current,
        string selectedProviderId,
        string selectedModelId,
        string? apiKey)
    {
        using var context = dbContextProvider.CreateDbContext();
        var configuration = context.WorkbenchConfigurations.Single(item => item.ConfigurationId == SingletonId);
        configuration.SelectedProviderId = selectedProviderId;
        configuration.SelectedModelId = selectedModelId;

        var keyEntity = context.ProviderApiKeys.SingleOrDefault(item => item.ProviderId == selectedProviderId);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (keyEntity is not null)
                context.ProviderApiKeys.Remove(keyEntity);
        }
        else if (keyEntity is null)
        {
            context.ProviderApiKeys.Add(new ProviderApiKeyEntity { ProviderId = selectedProviderId, ApiKey = apiKey.Trim() });
        }
        else
        {
            keyEntity.ApiKey = apiKey.Trim();
        }

        context.SaveChanges();
        return BuildRecord(context, current.WorkspacePath);
    }

    private static void SyncCatalog(LocalWorkbenchDbContext context)
    {
        foreach (var provider in LocalWorkbenchSeedCatalog.Providers.Select((item, index) => (item, index)))
        {
            var providerEntity = context.ProviderCatalog.SingleOrDefault(item => item.ProviderId == provider.item.Id);
            if (providerEntity is null)
            {
                providerEntity = new ProviderCatalogEntity { ProviderId = provider.item.Id };
                context.ProviderCatalog.Add(providerEntity);
            }

            providerEntity.DisplayName = provider.item.DisplayName;
            providerEntity.Kind = provider.item.Kind.ToString();
            providerEntity.Endpoint = provider.item.Endpoint;
            providerEntity.OrganizationId = provider.item.OrganizationId;
            providerEntity.IsDefault = provider.item.IsDefault;
            providerEntity.SortOrder = provider.index;

            foreach (var model in provider.item.Models.Select((item, index) => (item, index)))
            {
                var modelEntity = context.ProviderModels.SingleOrDefault(item => item.ProviderId == provider.item.Id && item.ModelId == model.item.Id);
                if (modelEntity is null)
                {
                    modelEntity = new ProviderModelEntity { ProviderId = provider.item.Id, ModelId = model.item.Id };
                    context.ProviderModels.Add(modelEntity);
                }

                modelEntity.DisplayName = model.item.DisplayName;
                modelEntity.IsDefault = model.item.IsDefault;
                modelEntity.SortOrder = model.index;
            }
        }

        var seededProviderIds = LocalWorkbenchSeedCatalog.Providers.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seededModelKeys = LocalWorkbenchSeedCatalog.Providers
            .SelectMany(provider => provider.Models.Select(model => $"{provider.Id}|{model.Id}"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var staleModels = context.ProviderModels
            .AsEnumerable()
            .Where(item => !seededModelKeys.Contains($"{item.ProviderId}|{item.ModelId}"))
            .ToArray();
        context.ProviderModels.RemoveRange(staleModels);

        var staleProviders = context.ProviderCatalog
            .AsEnumerable()
            .Where(item => !seededProviderIds.Contains(item.ProviderId))
            .ToArray();
        context.ProviderCatalog.RemoveRange(staleProviders);

        context.SaveChanges();
    }

    private static LocalWorkbenchConfigurationRecord BuildRecord(LocalWorkbenchDbContext context, string defaultWorkspacePath)
    {
        var providers = context.ProviderCatalog
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.DisplayName)
            .Select(provider => new AgentProviderRegistration
            {
                Id = provider.ProviderId,
                DisplayName = provider.DisplayName,
                Kind = Enum.Parse<AiProviderKind>(provider.Kind),
                Endpoint = provider.Endpoint,
                OrganizationId = provider.OrganizationId,
                IsDefault = provider.IsDefault,
                Models = context.ProviderModels
                    .AsNoTracking()
                    .Where(model => model.ProviderId == provider.ProviderId)
                    .OrderBy(model => model.SortOrder)
                    .ThenBy(model => model.DisplayName)
                    .Select(model => new AgentModelRegistration
                    {
                        Id = model.ModelId,
                        DisplayName = model.DisplayName,
                        IsDefault = model.IsDefault
                    })
                    .ToArray()
            })
            .ToArray();

        var apiKeys = context.ProviderApiKeys
            .AsNoTracking()
            .ToDictionary(item => item.ProviderId, item => item.ApiKey, StringComparer.OrdinalIgnoreCase);

        var configuration = context.WorkbenchConfigurations.SingleOrDefault(item => item.ConfigurationId == SingletonId);
        if (configuration is null)
        {
            var defaultProvider = providers.FirstOrDefault(item => item.IsDefault) ?? providers.First();
            var defaultModel = defaultProvider.Models.FirstOrDefault(item => item.IsDefault) ?? defaultProvider.Models.First();
            configuration = new WorkbenchConfigurationEntity
            {
                ConfigurationId = SingletonId,
                WorkspacePath = defaultWorkspacePath,
                SelectedProviderId = defaultProvider.Id,
                SelectedModelId = defaultModel.Id
            };
            context.WorkbenchConfigurations.Add(configuration);
            context.SaveChanges();
        }

        var workspacePath = Directory.Exists(configuration.WorkspacePath)
            ? configuration.WorkspacePath
            : defaultWorkspacePath;

        EnsureKnownWorkspace(context, workspacePath);

        var providerSelection = providers.FirstOrDefault(item => string.Equals(item.Id, configuration.SelectedProviderId, StringComparison.OrdinalIgnoreCase))
            ?? providers.First();
        var modelSelection = providerSelection.Models.FirstOrDefault(item => string.Equals(item.Id, configuration.SelectedModelId, StringComparison.OrdinalIgnoreCase))
            ?? providerSelection.Models.FirstOrDefault(item => item.IsDefault)
            ?? providerSelection.Models.First();

        var knownWorkspacePaths = context.KnownWorkspaces
            .AsNoTracking()
            .ToArray()
            .OrderByDescending(item => item.LastUsedUtc)
            .ThenBy(item => item.WorkspacePath)
            .Select(item => item.WorkspacePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new LocalWorkbenchConfigurationRecord(
            providers,
            knownWorkspacePaths,
            workspacePath,
            providerSelection.Id,
            modelSelection.Id,
            apiKeys);
    }

    private static void EnsureKnownWorkspace(LocalWorkbenchDbContext context, string workspacePath)
    {
        UpsertKnownWorkspace(context, workspacePath);
        var configuration = context.WorkbenchConfigurations.Single(item => item.ConfigurationId == SingletonId);
        configuration.WorkspacePath = workspacePath;
        context.SaveChanges();
    }

    private static void UpsertKnownWorkspace(LocalWorkbenchDbContext context, string workspacePath)
    {
        var knownWorkspace = context.KnownWorkspaces.SingleOrDefault(item => item.WorkspacePath == workspacePath);
        if (knownWorkspace is null)
        {
            context.KnownWorkspaces.Add(new KnownWorkspaceEntity
            {
                WorkspacePath = workspacePath,
                LastUsedUtc = DateTimeOffset.UtcNow
            });
        }
        else
        {
            knownWorkspace.LastUsedUtc = DateTimeOffset.UtcNow;
        }

        context.SaveChanges();
    }
}
