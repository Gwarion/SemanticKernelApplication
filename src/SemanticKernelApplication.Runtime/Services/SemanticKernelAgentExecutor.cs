using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.MistralAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Tools.Configuration;
using SemanticKernelApplication.Tools.Kernel;
using SemanticKernelApplication.Tools.Providers;
using Microsoft.Extensions.Options;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class SemanticKernelAgentExecutor : IAgentExecutor
{
    private readonly IAiProviderCatalog _providerCatalog;
    private readonly AgentProviderOptions _providerOptions;
    private readonly IWorkspacePluginCatalog _pluginCatalog;
    private readonly IProviderSessionConfiguration _providerSessionConfiguration;

    public SemanticKernelAgentExecutor(
        IAiProviderCatalog providerCatalog,
        IOptions<AgentProviderOptions> providerOptions,
        IWorkspacePluginCatalog pluginCatalog,
        IProviderSessionConfiguration providerSessionConfiguration)
    {
        _providerCatalog = providerCatalog;
        _providerOptions = providerOptions.Value;
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

        if (provider is null || provider.Kind == AiProviderKind.Demo || !provider.IsConfigured)
        {
            return BuildFallbackResult(request, startedAt, provider);
        }

        try
        {
            var registration = _providerSessionConfiguration.ResolveSelectedProvider(_providerOptions.Providers);
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
            return new AgentExecutionResult(
                request.OperationId,
                AgentExecutionStatus.Failed,
                Summary: $"Execution failed for {request.Agent.DisplayName}",
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
            AiProviderKind.Mistral => new MistralAIPromptExecutionSettings
            {
                ToolCallBehavior = MistralAIToolCallBehavior.AutoInvokeKernelFunctions
            },
            _ => null
        };
    }

    private Kernel BuildKernel(AgentProviderRegistration provider)
    {
        var builder = Kernel.CreateBuilder();

        switch (provider.Kind)
        {
            case AiProviderKind.OpenAI:
                builder.AddOpenAIChatCompletion(provider.ModelId, _providerSessionConfiguration.ApiKey!);
                break;
            case AiProviderKind.OpenAICompatible:
                builder.AddOpenAIChatCompletion(provider.ModelId, new Uri(provider.Endpoint!), _providerSessionConfiguration.ApiKey!, provider.OrganizationId, serviceId: provider.Id);
                break;
            case AiProviderKind.GoogleGemini:
                builder.AddGoogleAIGeminiChatCompletion(provider.ModelId, _providerSessionConfiguration.ApiKey!, serviceId: provider.Id);
                break;
            case AiProviderKind.Mistral:
                builder.AddMistralChatCompletion(provider.ModelId, _providerSessionConfiguration.ApiKey!, endpoint: string.IsNullOrWhiteSpace(provider.Endpoint) ? null : new Uri(provider.Endpoint), serviceId: provider.Id);
                break;
            case AiProviderKind.Anthropic:
                throw new InvalidOperationException("Anthropic is configured in the provider catalog, but this starter does not yet include a first-party Semantic Kernel Anthropic connector.");
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
            ? "No AI provider is configured, so the runtime used the built-in fallback coordinator summary."
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
        var agentDescription = request.Metadata?.GetValueOrDefault("agentDescription") ?? request.Agent.DisplayName;
        return $"""
            You are {request.Agent.DisplayName}.
            Public role description: {agentDescription}
            When a task requires workspace changes, file edits, or command execution, use the available tools instead of only describing what you would do.
            Keep all tool usage constrained to the configured workspace.
            Respond with useful, concise output suitable for a shared team activity panel.
            Do not reveal hidden chain-of-thought. Summarize your reasoning briefly and focus on next steps.
            """;
    }
}
