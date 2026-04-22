using System.Text.Json.Nodes;
using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Web.Services;

namespace SemanticKernelApplication.Web.Services;

public sealed class RuntimeActivityBridgeHostedService : BackgroundService
{
    private readonly IActivityStreamReader _activityStreamReader;
    private readonly IActivityEventStream _activityEventStream;

    public RuntimeActivityBridgeHostedService(
        IActivityStreamReader activityStreamReader,
        IActivityEventStream activityEventStream)
    {
        _activityStreamReader = activityStreamReader;
        _activityEventStream = activityEventStream;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var envelope in _activityStreamReader.ReadAllAsync(stoppingToken))
        {
            await _activityEventStream.PublishAsync(
                new ActivityEvent(
                    Type: envelope.Entry.Kind.ToString(),
                    Source: envelope.Entry.AgentId?.ToString("N") ?? envelope.Entry.Title,
                    ActivityId: envelope.Entry.Sequence.ToString(),
                    Stream: envelope.StreamId,
                    Message: envelope.Entry.Message,
                    Status: envelope.Entry.Status.ToString(),
                    Data: BuildData(envelope.Entry)),
                stoppingToken);
        }
    }

    private static JsonObject BuildData(ActivityLogEntry entry)
    {
        var data = new JsonObject
        {
            ["title"] = entry.Title,
            ["severity"] = entry.Severity.ToString(),
            ["activityKind"] = entry.Kind.ToString(),
            ["agentId"] = entry.AgentId?.ToString("N"),
            ["conversationId"] = entry.ConversationId?.ToString("N"),
            ["turnId"] = entry.TurnId?.ToString("N"),
            ["delta"] = entry.Delta
        };

        if (entry.Metadata is not null)
        {
            foreach (var pair in entry.Metadata)
            {
                data[pair.Key] = pair.Value;
            }
        }

        return data;
    }
}
