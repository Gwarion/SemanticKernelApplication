using System.Text.Json.Nodes;

namespace SemanticKernelApplication.Web.Services;

/// <summary>
/// Represents one activity event payload sent to the browser.
/// </summary>
/// <param name="Type">Event type identifier.</param>
/// <param name="Source">Optional event source label.</param>
/// <param name="ActivityId">Optional activity identifier.</param>
/// <param name="Stream">Optional stream identifier.</param>
/// <param name="Message">Optional human-readable message.</param>
/// <param name="Status">Optional event status.</param>
/// <param name="Data">Optional structured event payload.</param>
public sealed record ActivityEvent(
    string Type,
    string? Source = null,
    string? ActivityId = null,
    string? Stream = null,
    string? Message = null,
    string? Status = null,
    JsonObject? Data = null);
