namespace SemanticKernelApplication.Persistence.Entities;

internal sealed class ProviderCatalogEntity
{
    public string ProviderId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? OrganizationId { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public List<ProviderModelEntity> Models { get; set; } = [];
}
