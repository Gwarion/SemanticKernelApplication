namespace SemanticKernelApplication.Abstractions.Activities;

/// <summary>
/// Wraps an activity entry with the stream it belongs to.
/// </summary>
/// <param name="StreamId">Identifier of the activity stream.</param>
/// <param name="Entry">The activity entry being published.</param>
public sealed record ActivityStreamEnvelope(
    string StreamId,
    ActivityLogEntry Entry);
