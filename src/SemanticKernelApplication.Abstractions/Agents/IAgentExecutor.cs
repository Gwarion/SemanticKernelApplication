namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Executes agent requests against the configured model runtime.
/// </summary>
public interface IAgentExecutor
{
    /// <summary>
    /// Runs an agent request and returns the execution result.
    /// </summary>
    /// <param name="request">Request describing the agent work to perform.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionRequest request,
        CancellationToken cancellationToken = default);
}
