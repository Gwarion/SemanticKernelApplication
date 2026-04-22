using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Abstractions.Workbench;

namespace SemanticKernelApplication.Tools.Configuration;

public interface ILocalWorkbenchConfigurationStore
{
    IReadOnlyList<AgentProviderRegistration> GetProviders();

    AgentProviderRegistration? GetProvider(string? providerId);

    IReadOnlyList<string> GetKnownWorkspacePaths();

    GlobalModelConfiguration GetGlobalModelConfiguration();

    string? GetApiKey(string? providerId);

    string GetWorkspacePath();

    string SetWorkspacePath(string workspacePath);

    GlobalModelConfiguration UpdateGlobalModelConfiguration(GlobalModelConfigurationRequest request);
}

public sealed class LocalWorkbenchConfigurationStore : ILocalWorkbenchConfigurationStore
{
    private const string SingletonId = "main";

    private static readonly IReadOnlyList<AgentProviderRegistration> SeedProviders =
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
                new AgentModelRegistration { Id = "gpt-5.4-mini", DisplayName = "GPT-5.4 Mini" }
            ]
        },
        new AgentProviderRegistration
        {
            Id = "google",
            DisplayName = "Google Gemini",
            Kind = AiProviderKind.GoogleGemini,
            Models =
            [
                new AgentModelRegistration { Id = "gemini-2.5-flash", DisplayName = "Gemini 2.5 Flash", IsDefault = true },
                new AgentModelRegistration { Id = "gemini-2.5-pro", DisplayName = "Gemini 2.5 Pro" }
            ]
        },
        new AgentProviderRegistration
        {
            Id = "claude",
            DisplayName = "Claude",
            Kind = AiProviderKind.Anthropic,
            Models =
            [
                new AgentModelRegistration { Id = "claude-3-7-sonnet-latest", DisplayName = "Claude 3.7 Sonnet", IsDefault = true },
                new AgentModelRegistration { Id = "claude-3-5-haiku-latest", DisplayName = "Claude 3.5 Haiku" }
            ]
        }
    ];

    private readonly Lock _lock = new();
    private readonly string _connectionString;
    private readonly string _defaultWorkspacePath;

    private List<AgentProviderRegistration> _providers = [];
    private List<string> _knownWorkspacePaths = [];
    private string _workspacePath = string.Empty;
    private string _selectedProviderId = string.Empty;
    private string _selectedModelId = string.Empty;
    private readonly Dictionary<string, string> _apiKeys = new(StringComparer.OrdinalIgnoreCase);

    public LocalWorkbenchConfigurationStore(
        IOptions<LocalWorkbenchStoreOptions> storeOptions,
        IOptions<WorkspaceToolOptions> workspaceOptions)
    {
        var databasePath = ResolveDatabasePath(storeOptions.Value.DatabasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        _defaultWorkspacePath = NormalizeWorkspace(workspaceOptions.Value.RootPath);

        Initialize();
    }

    public IReadOnlyList<AgentProviderRegistration> GetProviders()
    {
        lock (_lock)
        {
            return _providers.ToArray();
        }
    }

    public AgentProviderRegistration? GetProvider(string? providerId)
    {
        lock (_lock)
        {
            var effectiveId = string.IsNullOrWhiteSpace(providerId) ? _selectedProviderId : providerId.Trim();
            return _providers.FirstOrDefault(provider => string.Equals(provider.Id, effectiveId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public GlobalModelConfiguration GetGlobalModelConfiguration()
    {
        lock (_lock)
        {
            return BuildConfigurationUnsafe();
        }
    }

    public IReadOnlyList<string> GetKnownWorkspacePaths()
    {
        lock (_lock)
        {
            return _knownWorkspacePaths.ToArray();
        }
    }

    public string? GetApiKey(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return null;
        }

        lock (_lock)
        {
            return _apiKeys.GetValueOrDefault(providerId.Trim());
        }
    }

    public string GetWorkspacePath()
    {
        lock (_lock)
        {
            return _workspacePath;
        }
    }

    public string SetWorkspacePath(string workspacePath)
    {
        var normalized = NormalizeWorkspace(workspacePath);

        lock (_lock)
        {
            _workspacePath = normalized;
            using var connection = OpenConnection();
            UpsertKnownWorkspace(connection, normalized);
            LoadKnownWorkspacePaths(connection);
            UpsertWorkbenchConfiguration(connection);
            return _workspacePath;
        }
    }

    public GlobalModelConfiguration UpdateGlobalModelConfiguration(GlobalModelConfigurationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SelectedProviderId))
        {
            throw new ArgumentException("A global provider selection is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SelectedModelId))
        {
            throw new ArgumentException("A model selection is required.", nameof(request));
        }

        lock (_lock)
        {
            var provider = _providers.FirstOrDefault(item => string.Equals(item.Id, request.SelectedProviderId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Provider '{request.SelectedProviderId}' was not found.");

            var model = provider.Models.FirstOrDefault(item => string.Equals(item.Id, request.SelectedModelId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Model '{request.SelectedModelId}' was not found for provider '{provider.DisplayName}'.");

            _selectedProviderId = provider.Id;
            _selectedModelId = model.Id;

            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                _apiKeys.Remove(provider.Id);
            }
            else
            {
                _apiKeys[provider.Id] = request.ApiKey.Trim();
            }

            using var connection = OpenConnection();
            UpsertWorkbenchConfiguration(connection);
            UpsertApiKey(connection, provider.Id, _apiKeys.GetValueOrDefault(provider.Id));

            return BuildConfigurationUnsafe();
        }
    }

    private void Initialize()
    {
        lock (_lock)
        {
            using var connection = OpenConnection();
            EnsureSchema(connection);
            SeedCatalog(connection);
            LoadProviders(connection);
            LoadConfiguration(connection);
            UpsertKnownWorkspace(connection, _workspacePath);
            LoadKnownWorkspacePaths(connection);
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static string ResolveDatabasePath(string databasePath)
    {
        var candidate = string.IsNullOrWhiteSpace(databasePath)
            ? ".appdata\\workbench.db"
            : databasePath.Trim();

        return Path.GetFullPath(candidate);
    }

    private static string NormalizeWorkspace(string workspacePath)
    {
        var candidate = string.IsNullOrWhiteSpace(workspacePath)
            ? Environment.CurrentDirectory
            : workspacePath.Trim();

        var fullPath = Path.GetFullPath(candidate);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Workspace folder '{fullPath}' does not exist.");
        }

        return fullPath;
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS provider_catalog (
                provider_id TEXT NOT NULL PRIMARY KEY,
                display_name TEXT NOT NULL,
                kind TEXT NOT NULL,
                endpoint TEXT NULL,
                organization_id TEXT NULL,
                is_default INTEGER NOT NULL,
                sort_order INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS provider_models (
                provider_id TEXT NOT NULL,
                model_id TEXT NOT NULL,
                display_name TEXT NOT NULL,
                is_default INTEGER NOT NULL,
                sort_order INTEGER NOT NULL,
                PRIMARY KEY (provider_id, model_id)
            );

            CREATE TABLE IF NOT EXISTS workbench_configuration (
                configuration_id TEXT NOT NULL PRIMARY KEY,
                workspace_path TEXT NOT NULL,
                selected_provider_id TEXT NOT NULL,
                selected_model_id TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS known_workspaces (
                workspace_path TEXT NOT NULL PRIMARY KEY,
                last_used_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS provider_api_keys (
                provider_id TEXT NOT NULL PRIMARY KEY,
                api_key TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private static void SeedCatalog(SqliteConnection connection)
    {
        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM provider_catalog;";
        var count = Convert.ToInt32(countCommand.ExecuteScalar());
        if (count > 0)
        {
            return;
        }

        for (var providerIndex = 0; providerIndex < SeedProviders.Count; providerIndex++)
        {
            var provider = SeedProviders[providerIndex];

            using var providerCommand = connection.CreateCommand();
            providerCommand.CommandText =
                """
                INSERT INTO provider_catalog (provider_id, display_name, kind, endpoint, organization_id, is_default, sort_order)
                VALUES ($providerId, $displayName, $kind, $endpoint, $organizationId, $isDefault, $sortOrder);
                """;
            providerCommand.Parameters.AddWithValue("$providerId", provider.Id);
            providerCommand.Parameters.AddWithValue("$displayName", provider.DisplayName);
            providerCommand.Parameters.AddWithValue("$kind", provider.Kind.ToString());
            providerCommand.Parameters.AddWithValue("$endpoint", (object?)provider.Endpoint ?? DBNull.Value);
            providerCommand.Parameters.AddWithValue("$organizationId", (object?)provider.OrganizationId ?? DBNull.Value);
            providerCommand.Parameters.AddWithValue("$isDefault", provider.IsDefault ? 1 : 0);
            providerCommand.Parameters.AddWithValue("$sortOrder", providerIndex);
            providerCommand.ExecuteNonQuery();

            for (var modelIndex = 0; modelIndex < provider.Models.Count; modelIndex++)
            {
                var model = provider.Models[modelIndex];
                using var modelCommand = connection.CreateCommand();
                modelCommand.CommandText =
                    """
                    INSERT INTO provider_models (provider_id, model_id, display_name, is_default, sort_order)
                    VALUES ($providerId, $modelId, $displayName, $isDefault, $sortOrder);
                    """;
                modelCommand.Parameters.AddWithValue("$providerId", provider.Id);
                modelCommand.Parameters.AddWithValue("$modelId", model.Id);
                modelCommand.Parameters.AddWithValue("$displayName", model.DisplayName);
                modelCommand.Parameters.AddWithValue("$isDefault", model.IsDefault ? 1 : 0);
                modelCommand.Parameters.AddWithValue("$sortOrder", modelIndex);
                modelCommand.ExecuteNonQuery();
            }
        }
    }

    private void LoadProviders(SqliteConnection connection)
    {
        var providerRows = new List<(string Id, string DisplayName, AiProviderKind Kind, string? Endpoint, string? OrganizationId, bool IsDefault)>();

        using var providerCommand = connection.CreateCommand();
        providerCommand.CommandText =
            """
            SELECT provider_id, display_name, kind, endpoint, organization_id, is_default
            FROM provider_catalog
            ORDER BY sort_order, display_name;
            """;

        using var reader = providerCommand.ExecuteReader();
        while (reader.Read())
        {
            providerRows.Add(
                (
                    reader.GetString(0),
                    reader.GetString(1),
                    Enum.Parse<AiProviderKind>(reader.GetString(2)),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.GetInt32(5) == 1));
        }

        _providers =
        [
            .. providerRows.Select(row => new AgentProviderRegistration
            {
                Id = row.Id,
                DisplayName = row.DisplayName,
                Kind = row.Kind,
                Endpoint = row.Endpoint,
                OrganizationId = row.OrganizationId,
                IsDefault = row.IsDefault,
                Models = LoadModels(connection, row.Id)
            })
        ];
    }

    private static IReadOnlyList<AgentModelRegistration> LoadModels(SqliteConnection connection, string providerId)
    {
        var models = new List<AgentModelRegistration>();

        using var modelCommand = connection.CreateCommand();
        modelCommand.CommandText =
            """
            SELECT model_id, display_name, is_default
            FROM provider_models
            WHERE provider_id = $providerId
            ORDER BY sort_order, display_name;
            """;
        modelCommand.Parameters.AddWithValue("$providerId", providerId);

        using var modelReader = modelCommand.ExecuteReader();
        while (modelReader.Read())
        {
            models.Add(
                new AgentModelRegistration
                {
                    Id = modelReader.GetString(0),
                    DisplayName = modelReader.GetString(1),
                    IsDefault = modelReader.GetInt32(2) == 1
                });
        }

        return models;
    }

    private void LoadConfiguration(SqliteConnection connection)
    {
        _apiKeys.Clear();

        using (var keyCommand = connection.CreateCommand())
        {
            keyCommand.CommandText = "SELECT provider_id, api_key FROM provider_api_keys;";
            using var keyReader = keyCommand.ExecuteReader();
            while (keyReader.Read())
            {
                _apiKeys[keyReader.GetString(0)] = keyReader.GetString(1);
            }
        }

        using var configCommand = connection.CreateCommand();
        configCommand.CommandText =
            """
            SELECT workspace_path, selected_provider_id, selected_model_id
            FROM workbench_configuration
            WHERE configuration_id = $configurationId
            LIMIT 1;
            """;
        configCommand.Parameters.AddWithValue("$configurationId", SingletonId);

        using var reader = configCommand.ExecuteReader();
        if (reader.Read())
        {
            var persistedWorkspacePath = reader.GetString(0);
            _workspacePath = Directory.Exists(persistedWorkspacePath)
                ? NormalizeWorkspace(persistedWorkspacePath)
                : _defaultWorkspacePath;
            _selectedProviderId = reader.GetString(1);
            _selectedModelId = reader.GetString(2);
            EnsureSelectionExists();
            UpsertWorkbenchConfiguration(connection);
            return;
        }

        var defaultProvider = _providers.FirstOrDefault(provider => provider.IsDefault) ?? _providers.First();
        var defaultModel = defaultProvider.Models.FirstOrDefault(model => model.IsDefault) ?? defaultProvider.Models.First();

        _workspacePath = _defaultWorkspacePath;
        _selectedProviderId = defaultProvider.Id;
        _selectedModelId = defaultModel.Id;

        UpsertWorkbenchConfiguration(connection);
    }

    private void LoadKnownWorkspacePaths(SqliteConnection connection)
    {
        var workspacePaths = new List<string>();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT workspace_path
            FROM known_workspaces
            ORDER BY last_used_utc DESC, workspace_path;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            workspacePaths.Add(reader.GetString(0));
        }

        if (workspacePaths.All(path => !string.Equals(path, _workspacePath, StringComparison.OrdinalIgnoreCase)))
        {
            workspacePaths.Insert(0, _workspacePath);
        }

        _knownWorkspacePaths = workspacePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void EnsureSelectionExists()
    {
        var provider = _providers.FirstOrDefault(item => string.Equals(item.Id, _selectedProviderId, StringComparison.OrdinalIgnoreCase))
            ?? _providers.First();

        var model = provider.Models.FirstOrDefault(item => string.Equals(item.Id, _selectedModelId, StringComparison.OrdinalIgnoreCase))
            ?? provider.Models.FirstOrDefault(item => item.IsDefault)
            ?? provider.Models.First();

        _selectedProviderId = provider.Id;
        _selectedModelId = model.Id;
    }

    private void UpsertWorkbenchConfiguration(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO workbench_configuration (configuration_id, workspace_path, selected_provider_id, selected_model_id)
            VALUES ($configurationId, $workspacePath, $selectedProviderId, $selectedModelId)
            ON CONFLICT(configuration_id) DO UPDATE SET
                workspace_path = excluded.workspace_path,
                selected_provider_id = excluded.selected_provider_id,
                selected_model_id = excluded.selected_model_id;
            """;
        command.Parameters.AddWithValue("$configurationId", SingletonId);
        command.Parameters.AddWithValue("$workspacePath", _workspacePath);
        command.Parameters.AddWithValue("$selectedProviderId", _selectedProviderId);
        command.Parameters.AddWithValue("$selectedModelId", _selectedModelId);
        command.ExecuteNonQuery();
    }

    private static void UpsertKnownWorkspace(SqliteConnection connection, string workspacePath)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO known_workspaces (workspace_path, last_used_utc)
            VALUES ($workspacePath, $lastUsedUtc)
            ON CONFLICT(workspace_path) DO UPDATE SET
                last_used_utc = excluded.last_used_utc;
            """;
        command.Parameters.AddWithValue("$workspacePath", workspacePath);
        command.Parameters.AddWithValue("$lastUsedUtc", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static void UpsertApiKey(SqliteConnection connection, string providerId, string? apiKey)
    {
        using var command = connection.CreateCommand();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            command.CommandText = "DELETE FROM provider_api_keys WHERE provider_id = $providerId;";
            command.Parameters.AddWithValue("$providerId", providerId);
            command.ExecuteNonQuery();
            return;
        }

        command.CommandText =
            """
            INSERT INTO provider_api_keys (provider_id, api_key)
            VALUES ($providerId, $apiKey)
            ON CONFLICT(provider_id) DO UPDATE SET
                api_key = excluded.api_key;
            """;
        command.Parameters.AddWithValue("$providerId", providerId);
        command.Parameters.AddWithValue("$apiKey", apiKey);
        command.ExecuteNonQuery();
    }

    private GlobalModelConfiguration BuildConfigurationUnsafe()
    {
        return new GlobalModelConfiguration(
            _selectedProviderId,
            _selectedModelId,
            _apiKeys.TryGetValue(_selectedProviderId, out var apiKey) && !string.IsNullOrWhiteSpace(apiKey),
            _apiKeys.GetValueOrDefault(_selectedProviderId));
    }
}
