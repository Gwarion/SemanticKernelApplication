using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using SemanticKernelApplication.Web.Services;

namespace SemanticKernelApplication.Web.Endpoints;

public static class ActivityStreamEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapActivityStreamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/activity");

        group.MapGet("/stream", StreamAsync)
            .WithName("ActivityStream");

        group.MapGet("/recent", GetRecent)
            .WithName("RecentActivityEvents");

        return endpoints;
    }

    private static async Task StreamAsync(
        HttpContext httpContext,
        IActivityEventStream activityEventStream,
        string? stream,
        int? replay,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";
        httpContext.Response.Headers["X-Accel-Buffering"] = "no";
        httpContext.Response.ContentType = "text/event-stream";

        await httpContext.Response.WriteAsync("retry: 2000\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);

        var eventEnumerator = activityEventStream
            .SubscribeAsync(stream, replay ?? 0, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var moveNextTask = eventEnumerator.MoveNextAsync().AsTask();
                var completedTask = await Task.WhenAny(moveNextTask, Task.Delay(TimeSpan.FromSeconds(15), cancellationToken));

                if (completedTask == moveNextTask)
                {
                    if (!await moveNextTask)
                    {
                        break;
                    }

                    var payload = JsonSerializer.Serialize(eventEnumerator.Current, SerializerOptions);
                    await httpContext.Response.WriteAsync($"id: {eventEnumerator.Current.Sequence}\n", cancellationToken);
                    await httpContext.Response.WriteAsync("event: activity\n", cancellationToken);
                    await httpContext.Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                }
                else
                {
                    await httpContext.Response.WriteAsync(": keepalive\n\n", cancellationToken);
                }

                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        }
        finally
        {
            await eventEnumerator.DisposeAsync();
        }
    }

    private static Ok<IReadOnlyList<ActivityEventEnvelope>> GetRecent(
        IActivityEventStream activityEventStream,
        string? stream,
        int? count)
    {
        var events = activityEventStream.GetRecentEvents(stream, count ?? 50);
        return TypedResults.Ok(events);
    }
}
