namespace SemanticKernelApplication.Abstractions.Activities;

/// <summary>
/// Publishes activity events to the application's activity stream.
/// </summary>
public interface IActivitySink
{
    /// <summary>
    /// Writes an activity envelope to the sink.
    /// </summary>
    /// <param name="envelope">The event payload to publish.</param>
    /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
    ValueTask PublishAsync(ActivityStreamEnvelope envelope, CancellationToken cancellationToken = default);
}
