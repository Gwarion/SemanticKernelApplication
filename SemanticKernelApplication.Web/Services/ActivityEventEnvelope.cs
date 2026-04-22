using System.Text.Json.Nodes;

namespace SemanticKernelApplication.Web.Services;

/// <summary>
/// Wraps an activity event with transport metadata used by the stream.
/// </summary>
/// <param name="Sequence">Monotonic sequence number assigned to the event.</param>
/// <param name="Timestamp">Timestamp when the event entered the stream.</param>
/// <param name="Type">Event type identifier.</param>
/// <param name="Source">Optional event source label.</param>
/// <param name="ActivityId">Optional activity identifier.</param>
/// <param name="Stream">Optional stream identifier.</param>
/// <param name="Message">Optional human-readable message.</param>
/// <param name="Status">Optional event status.</param>
/// <param name="Data">Optional structured event payload.</param>
public sealed record ActivityEventEnvelope(
    long Sequence,
    DateTimeOffset Timestamp,
    string Type,
    string? Source,
    string? ActivityId,
    string? Stream,
    string? Message,
    string? Status,
    JsonObject? Data);
