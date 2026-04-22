using Microsoft.EntityFrameworkCore;
using SemanticKernelApplication.Persistence.Entities;

namespace SemanticKernelApplication.Persistence;

internal sealed class LocalWorkbenchDbContext(DbContextOptions<LocalWorkbenchDbContext> options) : DbContext(options)
{
    public DbSet<ProviderCatalogEntity> ProviderCatalog => Set<ProviderCatalogEntity>();
    public DbSet<ProviderModelEntity> ProviderModels => Set<ProviderModelEntity>();
    public DbSet<WorkbenchConfigurationEntity> WorkbenchConfigurations => Set<WorkbenchConfigurationEntity>();
    public DbSet<KnownWorkspaceEntity> KnownWorkspaces => Set<KnownWorkspaceEntity>();
    public DbSet<ProviderApiKeyEntity> ProviderApiKeys => Set<ProviderApiKeyEntity>();
    public DbSet<ConversationThreadEntity> ConversationThreads => Set<ConversationThreadEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProviderCatalogEntity>(entity =>
        {
            entity.ToTable("provider_catalog");
            entity.HasKey(item => item.ProviderId);
            entity.Property(item => item.ProviderId).HasColumnName("provider_id");
            entity.Property(item => item.DisplayName).HasColumnName("display_name");
            entity.Property(item => item.Kind).HasColumnName("kind");
            entity.Property(item => item.Endpoint).HasColumnName("endpoint");
            entity.Property(item => item.OrganizationId).HasColumnName("organization_id");
            entity.Property(item => item.IsDefault).HasColumnName("is_default");
            entity.Property(item => item.SortOrder).HasColumnName("sort_order");
            entity.HasMany(item => item.Models)
                .WithOne(item => item.Provider)
                .HasForeignKey(item => item.ProviderId);
        });

        modelBuilder.Entity<ProviderModelEntity>(entity =>
        {
            entity.ToTable("provider_models");
            entity.HasKey(item => new { item.ProviderId, item.ModelId });
            entity.Property(item => item.ProviderId).HasColumnName("provider_id");
            entity.Property(item => item.ModelId).HasColumnName("model_id");
            entity.Property(item => item.DisplayName).HasColumnName("display_name");
            entity.Property(item => item.IsDefault).HasColumnName("is_default");
            entity.Property(item => item.SortOrder).HasColumnName("sort_order");
        });

        modelBuilder.Entity<WorkbenchConfigurationEntity>(entity =>
        {
            entity.ToTable("workbench_configuration");
            entity.HasKey(item => item.ConfigurationId);
            entity.Property(item => item.ConfigurationId).HasColumnName("configuration_id");
            entity.Property(item => item.WorkspacePath).HasColumnName("workspace_path");
            entity.Property(item => item.SelectedProviderId).HasColumnName("selected_provider_id");
            entity.Property(item => item.SelectedModelId).HasColumnName("selected_model_id");
        });

        modelBuilder.Entity<KnownWorkspaceEntity>(entity =>
        {
            entity.ToTable("known_workspaces");
            entity.HasKey(item => item.WorkspacePath);
            entity.Property(item => item.WorkspacePath).HasColumnName("workspace_path");
            entity.Property(item => item.LastUsedUtc).HasColumnName("last_used_utc");
        });

        modelBuilder.Entity<ProviderApiKeyEntity>(entity =>
        {
            entity.ToTable("provider_api_keys");
            entity.HasKey(item => item.ProviderId);
            entity.Property(item => item.ProviderId).HasColumnName("provider_id");
            entity.Property(item => item.ApiKey).HasColumnName("api_key");
        });

        modelBuilder.Entity<ConversationThreadEntity>(entity =>
        {
            entity.ToTable("conversation_threads");
            entity.HasKey(item => item.ThreadId);
            entity.Property(item => item.ThreadId).HasColumnName("thread_id");
            entity.Property(item => item.Title).HasColumnName("title");
            entity.Property(item => item.State).HasColumnName("state");
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(item => item.PayloadJson).HasColumnName("payload_json");
        });
    }
}
