using System.Text.Json.Serialization;

namespace SemanticKernelApplication.Abstractions.Providers;

/// <summary>
/// Represents one provider and the models it exposes to the workbench.
/// </summary>
public sealed class ModelProviderDefinition
{
    [JsonConstructor]
    private ModelProviderDefinition(
        string id,
        string displayName,
        AiProviderKind kind,
        IReadOnlyList<ModelDefinition> models,
        string selectedModelId,
        bool isConfigured,
        string? savedApiKey,
        bool isDefault,
        IReadOnlyDictionary<string, string>? metadata)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        Models = models;
        SelectedModelId = selectedModelId;
        IsConfigured = isConfigured;
        SavedApiKey = savedApiKey;
        IsDefault = isDefault;
        Metadata = metadata;
    }

    public static ModelProviderDefinitionBuilder Builder => new();

    public string Id { get; }
    public string DisplayName { get; }
    public AiProviderKind Kind { get; }
    public IReadOnlyList<ModelDefinition> Models { get; }
    public string SelectedModelId { get; }
    public bool IsConfigured { get; }
    public string? SavedApiKey { get; }
    public bool IsDefault { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public sealed class ModelProviderDefinitionBuilder
    {
        private string? _id;
        private string? _displayName;
        private AiProviderKind? _kind;
        private IReadOnlyList<ModelDefinition>? _models;
        private string? _selectedModelId;
        private bool _isConfigured;
        private string? _savedApiKey;
        private bool _isDefault;
        private IReadOnlyDictionary<string, string>? _metadata;

        public ModelProviderDefinitionBuilder WithId(string id) { _id = id; return this; }
        public ModelProviderDefinitionBuilder WithDisplayName(string displayName) { _displayName = displayName; return this; }
        public ModelProviderDefinitionBuilder WithKind(AiProviderKind kind) { _kind = kind; return this; }
        public ModelProviderDefinitionBuilder WithModels(IReadOnlyList<ModelDefinition> models) { _models = models; return this; }
        public ModelProviderDefinitionBuilder WithSelectedModelId(string selectedModelId) { _selectedModelId = selectedModelId; return this; }
        public ModelProviderDefinitionBuilder WithConfigured(bool isConfigured) { _isConfigured = isConfigured; return this; }
        public ModelProviderDefinitionBuilder WithSavedApiKey(string? savedApiKey) { _savedApiKey = savedApiKey; return this; }
        public ModelProviderDefinitionBuilder WithDefault(bool isDefault) { _isDefault = isDefault; return this; }
        public ModelProviderDefinitionBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata) { _metadata = metadata; return this; }

        public ModelProviderDefinition Build()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(_id);
            ArgumentException.ThrowIfNullOrWhiteSpace(_displayName);
            if (_kind is null) throw new InvalidOperationException("Provider kind is required.");
            ArgumentNullException.ThrowIfNull(_models);
            ArgumentException.ThrowIfNullOrWhiteSpace(_selectedModelId);

            return new ModelProviderDefinition(_id, _displayName, _kind.Value, _models, _selectedModelId, _isConfigured, _savedApiKey, _isDefault, _metadata);
        }
    }
}
