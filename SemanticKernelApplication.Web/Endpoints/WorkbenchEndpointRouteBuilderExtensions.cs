using Microsoft.AspNetCore.Http.HttpResults;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Web.Endpoints;

public static class WorkbenchEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapWorkbenchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/workbench");

        group.MapGet("/snapshot", GetSnapshotAsync)
            .WithName("GetWorkbenchSnapshot");

        group.MapPost("/agents/from-text", CreateAgentAsync)
            .WithName("CreateAgentFromText");

        group.MapPost("/coordinator/chat", SendCoordinatorMessageAsync)
            .WithName("SendCoordinatorMessage");

        return endpoints;
    }

    private static Task<Ok<WorkbenchSnapshot>> GetSnapshotAsync(
        IAgentWorkbenchService service,
        CancellationToken cancellationToken)
        => GetOkAsync(service.GetSnapshotAsync(cancellationToken));

    private static Task<Ok<AgentDefinition>> CreateAgentAsync(
        PlainTextAgentCreationRequest request,
        IAgentWorkbenchService service,
        CancellationToken cancellationToken)
        => GetOkAsync(service.CreateAgentFromTextAsync(request, cancellationToken));

    private static Task<Ok<CoordinatorChatResponse>> SendCoordinatorMessageAsync(
        CoordinatorChatRequest request,
        IAgentWorkbenchService service,
        CancellationToken cancellationToken)
        => GetOkAsync(service.SendCoordinatorMessageAsync(request, cancellationToken));

    private static async Task<Ok<T>> GetOkAsync<T>(Task<T> task)
    {
        var result = await task;
        return TypedResults.Ok(result);
    }
}
