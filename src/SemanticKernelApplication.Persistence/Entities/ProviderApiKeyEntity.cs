namespace SemanticKernelApplication.Persistence.Entities;

internal sealed class ProviderApiKeyEntity
{
    public string ProviderId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
