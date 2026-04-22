namespace SemanticKernelApplication.Persistence.Entities;

internal sealed class ProviderModelEntity
{
    public string ProviderId { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public ProviderCatalogEntity? Provider { get; set; }
}
