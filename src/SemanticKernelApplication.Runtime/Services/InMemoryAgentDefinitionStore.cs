using System.Collections.Concurrent;
using SemanticKernelApplication.Abstractions.Agents;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class InMemoryAgentDefinitionStore : IAgentDefinitionStore
{
    private readonly ConcurrentDictionary<Guid, AgentDefinition> _agents = [];

    public Task<AgentDefinition?> GetAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task<IReadOnlyList<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<AgentDefinition> agents = _agents.Values
            .OrderByDescending(agent => agent.UpdatedAtUtcOrNow)
            .ToArray();

        return Task.FromResult(agents);
    }

    public Task<AgentDefinition> UpsertAsync(AgentDefinition definition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        var normalized = definition
            .ToBuilder()
            .WithId(definition.Id == Guid.Empty ? Guid.NewGuid() : definition.Id)
            .WithCreatedAtUtc(definition.CreatedAtUtc ?? now)
            .WithUpdatedAtUtc(now)
            .Build();

        _agents[normalized.Id] = normalized;
        return Task.FromResult(normalized);
    }
}
