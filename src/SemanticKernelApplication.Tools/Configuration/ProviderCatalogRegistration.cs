using SemanticKernelApplication.Abstractions.Providers;

namespace SemanticKernelApplication.Tools.Configuration;

public sealed class AgentProviderRegistration
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public AiProviderKind Kind { get; init; }

    public string? Endpoint { get; init; }

    public string? OrganizationId { get; init; }

    public bool IsDefault { get; init; }

    public IReadOnlyList<AgentModelRegistration> Models { get; init; } = [];
}

public sealed class AgentModelRegistration
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsDefault { get; init; }
}
