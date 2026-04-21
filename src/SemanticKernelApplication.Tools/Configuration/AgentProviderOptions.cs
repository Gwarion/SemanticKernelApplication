using SemanticKernelApplication.Abstractions.Providers;

namespace SemanticKernelApplication.Tools.Configuration;

public sealed class AgentProviderOptions
{
    public List<AgentProviderRegistration> Providers { get; set; } = [];
}

public sealed class AgentProviderRegistration
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public AiProviderKind Kind { get; set; } = AiProviderKind.Demo;

    public string ModelId { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public string? Endpoint { get; set; }

    public string? Deployment { get; set; }

    public string? OrganizationId { get; set; }

    public bool IsDefault { get; set; }
}
