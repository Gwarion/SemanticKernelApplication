namespace SemanticKernelApplication.Abstractions.Providers;

public enum AiProviderKind
{
    OpenAI,
    AzureOpenAI,
    OpenAICompatible,
    GoogleGemini,
    Mistral,
    Anthropic
}

public sealed record ModelDefinition(
    string Id,
    string DisplayName,
    bool IsDefault = false);

public sealed record ModelProviderDefinition(
    string Id,
    string DisplayName,
    AiProviderKind Kind,
    IReadOnlyList<ModelDefinition> Models,
    string SelectedModelId,
    bool IsConfigured,
    string? SavedApiKey = null,
    bool IsDefault = false,
    IReadOnlyDictionary<string, string>? Metadata = null);
