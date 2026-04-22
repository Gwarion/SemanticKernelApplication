namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Stores and retrieves agent definitions for the workbench.
/// </summary>
public interface IAgentDefinitionStore
{
    /// <summary>
    /// Lists all stored agent definitions.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single agent definition by identifier.
    /// </summary>
    /// <param name="agentId">Identifier of the agent to fetch.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<AgentDefinition?> GetAsync(Guid agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates an agent definition.
    /// </summary>
    /// <param name="definition">Definition to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<AgentDefinition> UpsertAsync(AgentDefinition definition, CancellationToken cancellationToken = default);
}
