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

        if (!await TryWriteAsync(httpContext, "retry: 2000\n\n", cancellationToken))
        {
            return;
        }

        var eventEnumerator = activityEventStream
            .SubscribeAsync(stream, replay ?? 0, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        try
        {
            var moveNextTask = eventEnumerator.MoveNextAsync().AsTask();

            while (!cancellationToken.IsCancellationRequested)
            {
                var completedTask = await Task.WhenAny(moveNextTask, Task.Delay(TimeSpan.FromSeconds(15), cancellationToken));

                if (completedTask == moveNextTask)
                {
                    if (!await moveNextTask)
                    {
                        break;
                    }

                    var payload = JsonSerializer.Serialize(eventEnumerator.Current, SerializerOptions);
                    if (!await TryWriteAsync(httpContext, $"id: {eventEnumerator.Current.Sequence}\n", cancellationToken) ||
                        !await TryWriteAsync(httpContext, "event: activity\n", cancellationToken) ||
                        !await TryWriteAsync(httpContext, $"data: {payload}\n\n", cancellationToken))
                    {
                        break;
                    }

                    moveNextTask = eventEnumerator.MoveNextAsync().AsTask();
                }
                else
                {
                    if (!await TryWriteAsync(httpContext, ": keepalive\n\n", cancellationToken))
                    {
                        break;
                    }
                }

                if (!await TryFlushAsync(httpContext, cancellationToken))
                {
                    break;
                }
            }
        }
        finally
        {
            await eventEnumerator.DisposeAsync();
        }
    }

    private static async Task<bool> TryWriteAsync(HttpContext httpContext, string content, CancellationToken cancellationToken)
    {
        try
        {
            await httpContext.Response.WriteAsync(content, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private static async Task<bool> TryFlushAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            await httpContext.Response.Body.FlushAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
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
