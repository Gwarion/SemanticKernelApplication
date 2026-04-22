using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Persistence;

internal static class LocalWorkbenchSeedCatalog
{
    public static IReadOnlyList<AgentProviderRegistration> Providers { get; } =
    [
        new AgentProviderRegistration
        {
            Id = "openai",
            DisplayName = "OpenAI",
            Kind = AiProviderKind.OpenAI,
            IsDefault = true,
            Models =
            [
                new AgentModelRegistration { Id = "gpt-5.4", DisplayName = "GPT-5.4", IsDefault = true },
                new AgentModelRegistration { Id = "gpt-5.4-mini", DisplayName = "GPT-5.4 Mini" },
                new AgentModelRegistration { Id = "gpt-5.4-nano", DisplayName = "GPT-5.4 Nano" },
                new AgentModelRegistration { Id = "gpt-5.2", DisplayName = "GPT-5.2" },
                new AgentModelRegistration { Id = "gpt-5.1", DisplayName = "GPT-5.1" },
                new AgentModelRegistration { Id = "gpt-5", DisplayName = "GPT-5" },
                new AgentModelRegistration { Id = "gpt-4.1", DisplayName = "GPT-4.1" },
                new AgentModelRegistration { Id = "gpt-4.1-mini", DisplayName = "GPT-4.1 Mini" },
                new AgentModelRegistration { Id = "gpt-4.1-nano", DisplayName = "GPT-4.1 Nano" },
                new AgentModelRegistration { Id = "gpt-4o", DisplayName = "GPT-4o" },
                new AgentModelRegistration { Id = "gpt-4o-mini", DisplayName = "GPT-4o Mini" }
            ]
        },
        new AgentProviderRegistration
        {
            Id = "google",
            DisplayName = "Google Gemini",
            Kind = AiProviderKind.GoogleGemini,
            Models =
            [
                new AgentModelRegistration { Id = "gemini-3-pro-preview", DisplayName = "Gemini 3 Pro Preview", IsDefault = true },
                new AgentModelRegistration { Id = "gemini-3-flash-preview", DisplayName = "Gemini 3 Flash Preview" },
                new AgentModelRegistration { Id = "gemini-2.5-flash-preview-09-2025", DisplayName = "Gemini 2.5 Flash Preview" },
                new AgentModelRegistration { Id = "gemini-2.5-flash-lite-preview-09-2025", DisplayName = "Gemini 2.5 Flash-Lite Preview" },
                new AgentModelRegistration { Id = "gemini-2.5-pro", DisplayName = "Gemini 2.5 Pro" },
                new AgentModelRegistration { Id = "gemini-2.5-flash", DisplayName = "Gemini 2.5 Flash" },
                new AgentModelRegistration { Id = "gemini-2.5-flash-lite", DisplayName = "Gemini 2.5 Flash-Lite" },
                new AgentModelRegistration { Id = "gemini-2.0-flash", DisplayName = "Gemini 2.0 Flash" },
                new AgentModelRegistration { Id = "gemini-2.0-flash-lite", DisplayName = "Gemini 2.0 Flash-Lite" }
            ]
        },
        new AgentProviderRegistration
        {
            Id = "claude",
            DisplayName = "Claude",
            Kind = AiProviderKind.Anthropic,
            Models =
            [
                new AgentModelRegistration { Id = "claude-opus-4-7", DisplayName = "Claude Opus 4.7", IsDefault = true },
                new AgentModelRegistration { Id = "claude-sonnet-4-6", DisplayName = "Claude Sonnet 4.6" },
                new AgentModelRegistration { Id = "claude-haiku-4-5", DisplayName = "Claude Haiku 4.5" },
                new AgentModelRegistration { Id = "claude-opus-4-6", DisplayName = "Claude Opus 4.6" },
                new AgentModelRegistration { Id = "claude-sonnet-4-5", DisplayName = "Claude Sonnet 4.5" },
                new AgentModelRegistration { Id = "claude-3-7-sonnet-latest", DisplayName = "Claude 3.7 Sonnet" },
                new AgentModelRegistration { Id = "claude-3-5-sonnet-latest", DisplayName = "Claude 3.5 Sonnet" },
                new AgentModelRegistration { Id = "claude-3-5-haiku-latest", DisplayName = "Claude 3.5 Haiku" },
                new AgentModelRegistration { Id = "claude-3-haiku-20240307", DisplayName = "Claude 3 Haiku" }
            ]
        }
    ];
}
