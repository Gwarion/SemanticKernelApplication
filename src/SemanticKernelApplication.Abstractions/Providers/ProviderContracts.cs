namespace SemanticKernelApplication.Abstractions.Providers;

public enum AiProviderKind
{
    Demo,
    OpenAI,
    AzureOpenAI,
    OpenAICompatible,
    GoogleGemini,
    Mistral,
    Anthropic
}

public sealed record ModelProviderDefinition(
    string Id,
    string DisplayName,
    AiProviderKind Kind,
    string ModelId,
    bool IsConfigured,
    bool IsDefault = false,
    IReadOnlyDictionary<string, string>? Metadata = null);
