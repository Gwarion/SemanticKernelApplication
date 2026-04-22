using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Tools.Configuration;
using SemanticKernelApplication.Tools.Kernel;
using SemanticKernelApplication.Tools.Providers;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class SemanticKernelAgentExecutor : IAgentExecutor
{
    private readonly IAiProviderCatalog _providerCatalog;
    private readonly IWorkspacePluginCatalog _pluginCatalog;
    private readonly IProviderSessionConfiguration _providerSessionConfiguration;

    public SemanticKernelAgentExecutor(
        IAiProviderCatalog providerCatalog,
        IWorkspacePluginCatalog pluginCatalog,
        IProviderSessionConfiguration providerSessionConfiguration)
    {
        _providerCatalog = providerCatalog;
        _pluginCatalog = pluginCatalog;
        _providerSessionConfiguration = providerSessionConfiguration;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = DateTimeOffset.UtcNow;
        var provider = _providerCatalog.GetProvider(null);

        if (provider is null || !provider.IsConfigured)
        {
            return BuildFallbackResult(request, startedAt, provider);
        }

        AgentProviderRegistration? registration = null;
        try
        {
            registration = _providerCatalog.GetRegistration(null);
            if (registration is null)
            {
                return BuildFallbackResult(request, startedAt, provider);
            }

            var kernel = BuildKernel(registration);
            foreach (var plugin in _pluginCatalog.GetPlugins())
            {
                kernel.Plugins.AddFromObject(plugin.Instance, plugin.Name);
            }

            var chat = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory(BuildSystemPrompt(request));
            AppendConversationHistory(history, request.Metadata?.GetValueOrDefault("conversationHistory"));
            history.AddUserMessage(request.Input);
            var executionSettings = CreateExecutionSettings(registration);

            var response = await chat.GetChatMessageContentAsync(
                history,
                executionSettings,
                kernel,
                cancellationToken);
            var text = string.IsNullOrWhiteSpace(response.Content)
                ? $"Agent {request.Agent.DisplayName} completed without a visible message."
                : response.Content;

            return new AgentExecutionResult(
                request.OperationId,
                AgentExecutionStatus.Completed,
                text,
                $"Completed by {request.Agent.DisplayName}",
                StartedAtUtc: startedAt,
                CompletedAtUtc: DateTimeOffset.UtcNow);
        }
        catch (Exception exception)
        {
            var failureDetails = new Dictionary<string, string>
            {
                ["providerId"] = registration?.Id ?? provider?.Id ?? "unknown",
                ["providerKind"] = (registration?.Kind ?? provider?.Kind)?.ToString() ?? "unknown",
                ["modelId"] = _providerSessionConfiguration.SelectedModelId ?? provider?.SelectedModelId ?? "unknown",
                ["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name,
                ["exceptionMessage"] = exception.Message,
                ["exceptionDetails"] = exception.ToString()
            };

            return new AgentExecutionResult(
                request.OperationId,
                AgentExecutionStatus.Failed,
                Summary: $"Execution failed for {request.Agent.DisplayName}",
                Metadata: failureDetails,
                StartedAtUtc: startedAt,
                CompletedAtUtc: DateTimeOffset.UtcNow,
                FailureReason: $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static PromptExecutionSettings? CreateExecutionSettings(AgentProviderRegistration provider)
    {
        return provider.Kind switch
        {
            AiProviderKind.OpenAI or AiProviderKind.OpenAICompatible => new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            },
            AiProviderKind.GoogleGemini => new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            },
            _ => null
        };
    }

    private Kernel BuildKernel(AgentProviderRegistration provider)
    {
        var builder = Kernel.CreateBuilder();
        var modelId = _providerSessionConfiguration.SelectedModelId
            ?? provider.Models.FirstOrDefault(model => model.IsDefault)?.Id
            ?? provider.Models.First().Id;

        switch (provider.Kind)
        {
            case AiProviderKind.OpenAI:
                builder.AddOpenAIChatCompletion(modelId, _providerSessionConfiguration.ApiKey!);
                break;
            case AiProviderKind.OpenAICompatible:
                builder.AddOpenAIChatCompletion(modelId, new Uri(provider.Endpoint!), _providerSessionConfiguration.ApiKey!, provider.OrganizationId, serviceId: provider.Id);
                break;
            case AiProviderKind.GoogleGemini:
                builder.AddGoogleAIGeminiChatCompletion(modelId, _providerSessionConfiguration.ApiKey!, serviceId: provider.Id);
                break;
            case AiProviderKind.Anthropic:
                throw new InvalidOperationException("Claude is configured in the local catalog, but this build does not yet include the Semantic Kernel Anthropic connector package.");
            case AiProviderKind.AzureOpenAI:
                throw new InvalidOperationException("Azure OpenAI is not wired in this starter. Add the Azure OpenAI connector package and endpoint mapping to enable it.");
            default:
                throw new InvalidOperationException($"Provider kind '{provider.Kind}' is not supported.");
        }

        return builder.Build();
    }

    private static AgentExecutionResult BuildFallbackResult(
        AgentExecutionRequest request,
        DateTimeOffset startedAt,
        ModelProviderDefinition? provider)
    {
        var summary = provider is null
            ? "No configured model is available, so the runtime used the built-in fallback coordinator summary."
            : $"Provider '{provider.DisplayName}' is unavailable in this environment, so the runtime used the built-in fallback summary.";

        var output = $$"""
            {{request.Agent.DisplayName}} reviewed the request.
            Input: {{request.Input}}
            Result: {{summary}}
            """;

        return new AgentExecutionResult(
            request.OperationId,
            AgentExecutionStatus.Completed,
            output,
            summary,
            StartedAtUtc: startedAt,
            CompletedAtUtc: DateTimeOffset.UtcNow);
    }

    private static string BuildSystemPrompt(AgentExecutionRequest request)
    {
        if (request.Metadata?.TryGetValue("systemPrompt", out var systemPromptOverride) == true
            && !string.IsNullOrWhiteSpace(systemPromptOverride))
        {
            return systemPromptOverride;
        }

        var agentDescription = request.Metadata?.GetValueOrDefault("agentDescription") ?? request.Agent.DisplayName;
        var agentSystemPrompt = request.Metadata?.GetValueOrDefault("agentSystemPrompt");
        return $"""
            You are {request.Agent.DisplayName}.
            Public role description: {agentDescription}
            {agentSystemPrompt}
            When a task requires workspace changes, file edits, or command execution, use the available tools instead of only describing what you would do.
            Keep all tool usage constrained to the configured workspace.
            Respond with useful, concise output suitable for a shared team activity panel.
            Do not reveal hidden chain-of-thought. Summarize your reasoning briefly and focus on next steps.
            """;
    }

    private static void AppendConversationHistory(ChatHistory history, string? conversationHistory)
    {
        if (string.IsNullOrWhiteSpace(conversationHistory))
        {
            return;
        }

        history.AddSystemMessage(
            $$"""
            Recent conversation context:
            {{conversationHistory}}
            """);
    }
}
