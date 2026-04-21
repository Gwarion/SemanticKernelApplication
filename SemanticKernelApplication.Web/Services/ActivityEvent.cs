using System.Text.Json.Nodes;

namespace SemanticKernelApplication.Web.Services;

public sealed record ActivityEvent(
    string Type,
    string? Source = null,
    string? ActivityId = null,
    string? Stream = null,
    string? Message = null,
    string? Status = null,
    JsonObject? Data = null);

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
