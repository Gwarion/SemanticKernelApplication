using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Runtime.Services.Agents;

public sealed class AgentCreationService : IAgentCreationService
{
    private readonly IAgentDefinitionStore _agentDefinitionStore;
    private readonly PlainTextAgentDefinitionFactory _definitionFactory;
    private readonly IActivitySink _activitySink;

    public AgentCreationService(
        IAgentDefinitionStore agentDefinitionStore,
        PlainTextAgentDefinitionFactory definitionFactory,
        IActivitySink activitySink)
    {
        _agentDefinitionStore = agentDefinitionStore;
        _definitionFactory = definitionFactory;
        _activitySink = activitySink;
    }

    public async Task<AgentDefinition> CreateFromTextAsync(
        PlainTextAgentCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Agent description is required.", nameof(request));
        }

        var definition = _definitionFactory.Create(request);
        definition = await _agentDefinitionStore.UpsertAsync(definition, cancellationToken);

        await _activitySink.PublishAsync(
            new ActivityStreamEnvelope(
                "workbench",
                new ActivityLogEntry(
                    0,
                    ActivityKind.Status,
                    ActivityStatus.Completed,
                    ActivitySeverity.Information,
                    "Agent created",
                    $"{definition.Name} is ready for coordinator assignments.",
                    DateTimeOffset.UtcNow,
                    AgentId: definition.Id,
                    Metadata: new Dictionary<string, string> { ["providerId"] = definition.ProviderId ?? string.Empty })),
            cancellationToken);

        return definition;
    }
}
