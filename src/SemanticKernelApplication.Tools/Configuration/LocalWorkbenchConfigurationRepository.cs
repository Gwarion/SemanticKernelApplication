using Microsoft.Data.Sqlite;
using SemanticKernelApplication.Abstractions.Providers;

namespace SemanticKernelApplication.Tools.Configuration;

/// <summary>
/// Encapsulates SQLite persistence for local workbench configuration data.
/// </summary>
internal sealed class LocalWorkbenchConfigurationRepository
{
    private const string SingletonId = "main";
    private readonly string _connectionString;

    public LocalWorkbenchConfigurationRepository(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
    }

    public LocalWorkbenchConfigurationState LoadState(string defaultWorkspacePath, IReadOnlyList<AgentProviderRegistration> seedProviders)
    {
        using var connection = OpenConnection();
        EnsureSchema(connection);
        SyncCatalog(connection, seedProviders);

        var state = new LocalWorkbenchConfigurationState
        {
            Providers = LoadProviders(connection)
        };

        LoadConfiguration(connection, state, defaultWorkspacePath);
        UpsertKnownWorkspace(connection, state.WorkspacePath);
        state.KnownWorkspacePaths = LoadKnownWorkspacePaths(connection, state.WorkspacePath);
        return state;
    }

    public LocalWorkbenchConfigurationState PersistWorkspace(LocalWorkbenchConfigurationState state, string workspacePath)
    {
        using var connection = OpenConnection();
        UpsertKnownWorkspace(connection, workspacePath);
        state.WorkspacePath = workspacePath;
        UpsertWorkbenchConfiguration(connection, state);
        state.KnownWorkspacePaths = LoadKnownWorkspacePaths(connection, state.WorkspacePath);
        return state;
    }

    public void PersistGlobalConfiguration(LocalWorkbenchConfigurationState state, string providerId, string selectedModelId, string? apiKey)
    {
        state.SelectedProviderId = providerId;
        state.SelectedModelId = selectedModelId;

        if (string.IsNullOrWhiteSpace(apiKey))
            state.ApiKeys.Remove(providerId);
        else
            state.ApiKeys[providerId] = apiKey.Trim();

        using var connection = OpenConnection();
        UpsertWorkbenchConfiguration(connection, state);
        UpsertApiKey(connection, providerId, state.ApiKeys.GetValueOrDefault(providerId));
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
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

    private static void SyncCatalog(SqliteConnection connection, IReadOnlyList<AgentProviderRegistration> seedProviders)
    {
        for (var providerIndex = 0; providerIndex < seedProviders.Count; providerIndex++)
        {
            var provider = seedProviders[providerIndex];
            UpsertProvider(connection, provider, providerIndex);

            for (var modelIndex = 0; modelIndex < provider.Models.Count; modelIndex++)
                UpsertProviderModel(connection, provider.Id, provider.Models[modelIndex], modelIndex);
        }

        DeleteStaleModels(connection, seedProviders);
        DeleteStaleProviders(connection, seedProviders);
    }

    private static void UpsertProvider(SqliteConnection connection, AgentProviderRegistration provider, int sortOrder)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO provider_catalog (provider_id, display_name, kind, endpoint, organization_id, is_default, sort_order)
            VALUES ($providerId, $displayName, $kind, $endpoint, $organizationId, $isDefault, $sortOrder)
            ON CONFLICT(provider_id) DO UPDATE SET
                display_name = excluded.display_name,
                kind = excluded.kind,
                endpoint = excluded.endpoint,
                organization_id = excluded.organization_id,
                is_default = excluded.is_default,
                sort_order = excluded.sort_order;
            """;
        command.Parameters.AddWithValue("$providerId", provider.Id);
        command.Parameters.AddWithValue("$displayName", provider.DisplayName);
        command.Parameters.AddWithValue("$kind", provider.Kind.ToString());
        command.Parameters.AddWithValue("$endpoint", (object?)provider.Endpoint ?? DBNull.Value);
        command.Parameters.AddWithValue("$organizationId", (object?)provider.OrganizationId ?? DBNull.Value);
        command.Parameters.AddWithValue("$isDefault", provider.IsDefault ? 1 : 0);
        command.Parameters.AddWithValue("$sortOrder", sortOrder);
        command.ExecuteNonQuery();
    }

    private static void UpsertProviderModel(SqliteConnection connection, string providerId, AgentModelRegistration model, int sortOrder)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO provider_models (provider_id, model_id, display_name, is_default, sort_order)
            VALUES ($providerId, $modelId, $displayName, $isDefault, $sortOrder)
            ON CONFLICT(provider_id, model_id) DO UPDATE SET
                display_name = excluded.display_name,
                is_default = excluded.is_default,
                sort_order = excluded.sort_order;
            """;
        command.Parameters.AddWithValue("$providerId", providerId);
        command.Parameters.AddWithValue("$modelId", model.Id);
        command.Parameters.AddWithValue("$displayName", model.DisplayName);
        command.Parameters.AddWithValue("$isDefault", model.IsDefault ? 1 : 0);
        command.Parameters.AddWithValue("$sortOrder", sortOrder);
        command.ExecuteNonQuery();
    }

    private static void DeleteStaleModels(SqliteConnection connection, IReadOnlyList<AgentProviderRegistration> seedProviders)
    {
        var modelKeys = seedProviders
            .SelectMany(provider => provider.Models.Select(model => $"{provider.Id}|{model.Id}"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var staleModels = new List<(string ProviderId, string ModelId)>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT provider_id, model_id FROM provider_models;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var providerId = reader.GetString(0);
                var modelId = reader.GetString(1);
                if (!modelKeys.Contains($"{providerId}|{modelId}"))
                    staleModels.Add((providerId, modelId));
            }
        }

        foreach (var staleModel in staleModels)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM provider_models WHERE provider_id = $providerId AND model_id = $modelId;";
            command.Parameters.AddWithValue("$providerId", staleModel.ProviderId);
            command.Parameters.AddWithValue("$modelId", staleModel.ModelId);
            command.ExecuteNonQuery();
        }
    }

    private static void DeleteStaleProviders(SqliteConnection connection, IReadOnlyList<AgentProviderRegistration> seedProviders)
    {
        var providerIds = seedProviders.Select(provider => provider.Id).ToArray();
        var staleProviders = new List<string>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT provider_id FROM provider_catalog;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var providerId = reader.GetString(0);
                if (!providerIds.Contains(providerId, StringComparer.OrdinalIgnoreCase))
                    staleProviders.Add(providerId);
            }
        }

        foreach (var staleProvider in staleProviders)
        {
            using var deleteModels = connection.CreateCommand();
            deleteModels.CommandText = "DELETE FROM provider_models WHERE provider_id = $providerId;";
            deleteModels.Parameters.AddWithValue("$providerId", staleProvider);
            deleteModels.ExecuteNonQuery();

            using var deleteProvider = connection.CreateCommand();
            deleteProvider.CommandText = "DELETE FROM provider_catalog WHERE provider_id = $providerId;";
            deleteProvider.Parameters.AddWithValue("$providerId", staleProvider);
            deleteProvider.ExecuteNonQuery();
        }
    }

    private static List<AgentProviderRegistration> LoadProviders(SqliteConnection connection)
    {
        var providerRows = new List<(string Id, string DisplayName, AiProviderKind Kind, string? Endpoint, string? OrganizationId, bool IsDefault)>();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT provider_id, display_name, kind, endpoint, organization_id, is_default
            FROM provider_catalog
            ORDER BY sort_order, display_name;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            providerRows.Add((
                reader.GetString(0),
                reader.GetString(1),
                Enum.Parse<AiProviderKind>(reader.GetString(2)),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetInt32(5) == 1));
        }

        return
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

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT model_id, display_name, is_default
            FROM provider_models
            WHERE provider_id = $providerId
            ORDER BY sort_order, display_name;
            """;
        command.Parameters.AddWithValue("$providerId", providerId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            models.Add(new AgentModelRegistration
            {
                Id = reader.GetString(0),
                DisplayName = reader.GetString(1),
                IsDefault = reader.GetInt32(2) == 1
            });
        }

        return models;
    }

    private static void LoadConfiguration(SqliteConnection connection, LocalWorkbenchConfigurationState state, string defaultWorkspacePath)
    {
        state.ApiKeys.Clear();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT provider_id, api_key FROM provider_api_keys;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                state.ApiKeys[reader.GetString(0)] = reader.GetString(1);
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

        using var configReader = configCommand.ExecuteReader();
        if (!configReader.Read())
        {
            var defaultProvider = state.Providers.FirstOrDefault(provider => provider.IsDefault) ?? state.Providers.First();
            var defaultModel = defaultProvider.Models.FirstOrDefault(model => model.IsDefault) ?? defaultProvider.Models.First();
            state.WorkspacePath = defaultWorkspacePath;
            state.SelectedProviderId = defaultProvider.Id;
            state.SelectedModelId = defaultModel.Id;
            UpsertWorkbenchConfiguration(connection, state);
            return;
        }

        var persistedWorkspacePath = configReader.GetString(0);
        state.WorkspacePath = Directory.Exists(persistedWorkspacePath) ? LocalWorkbenchConfigurationStore.NormalizeWorkspace(persistedWorkspacePath) : defaultWorkspacePath;
        state.SelectedProviderId = configReader.GetString(1);
        state.SelectedModelId = configReader.GetString(2);
        state.EnsureSelectionExists();
        UpsertWorkbenchConfiguration(connection, state);
    }

    private static List<string> LoadKnownWorkspacePaths(SqliteConnection connection, string workspacePath)
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
            workspacePaths.Add(reader.GetString(0));

        if (workspacePaths.All(path => !string.Equals(path, workspacePath, StringComparison.OrdinalIgnoreCase)))
            workspacePaths.Insert(0, workspacePath);

        return workspacePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void UpsertWorkbenchConfiguration(SqliteConnection connection, LocalWorkbenchConfigurationState state)
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
        command.Parameters.AddWithValue("$workspacePath", state.WorkspacePath);
        command.Parameters.AddWithValue("$selectedProviderId", state.SelectedProviderId);
        command.Parameters.AddWithValue("$selectedModelId", state.SelectedModelId);
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
}
