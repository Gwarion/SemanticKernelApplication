namespace SemanticKernelApplication.Abstractions.Providers;

/// <summary>
/// Identifies the backing model provider family.
/// </summary>
public enum AiProviderKind
{
    OpenAI,
    AzureOpenAI,
    OpenAICompatible,
    GoogleGemini,
    Mistral,
    Anthropic
}
