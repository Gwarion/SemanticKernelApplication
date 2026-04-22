namespace SemanticKernelApplication.Abstractions.Orchestration;

/// <summary>
/// Executes coordinator workflows over a set of agents and a conversation thread.
/// </summary>
public interface ICoordinatorOrchestrator
{
    /// <summary>
    /// Runs a coordination workflow and returns the resulting conversation state.
    /// </summary>
    /// <param name="request">Workflow request to execute.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    Task<CoordinationResult> ExecuteAsync(
        CoordinationRequest request,
        CancellationToken cancellationToken = default);
}
