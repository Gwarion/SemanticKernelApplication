namespace SemanticKernelApplication.Abstractions.Activities;

/// <summary>
/// Reads activity events as an asynchronous stream.
/// </summary>
public interface IActivityStreamReader
{
    /// <summary>
    /// Streams all available activity envelopes until the reader completes or cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    IAsyncEnumerable<ActivityStreamEnvelope> ReadAllAsync(CancellationToken cancellationToken = default);
}
