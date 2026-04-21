using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SemanticKernelApplication.Web.Services;

public static class ActivityStreamingServiceCollectionExtensions
{
    public static IServiceCollection AddActivityStreaming(
        this IServiceCollection services,
        int maxHistory = 256)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IActivityEventStream>(_ => new ActivityEventStream(maxHistory));

        return services;
    }
}
