using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Tools.Configuration;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class LocalConversationStore : IConversationStore
{
    private readonly string _connectionString;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public LocalConversationStore(IOptions<LocalWorkbenchStoreOptions> storeOptions)
    {
        var databasePath = ResolveDatabasePath(storeOptions.Value.DatabasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        EnsureSchema();
    }

    public Task<ConversationThread?> GetAsync(string threadId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT payload_json
            FROM conversation_threads
            WHERE thread_id = $threadId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$threadId", threadId);

        var payload = command.ExecuteScalar() as string;
        var thread = string.IsNullOrWhiteSpace(payload)
            ? null
            : JsonSerializer.Deserialize<ConversationThread>(payload, JsonOptions);

        return Task.FromResult(thread);
    }

    public Task<ConversationThread> SaveAsync(ConversationThread thread, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO conversation_threads (
                thread_id,
                title,
                state,
                created_at_utc,
                updated_at_utc,
                payload_json)
            VALUES (
                $threadId,
                $title,
                $state,
                $createdAtUtc,
                $updatedAtUtc,
                $payloadJson)
            ON CONFLICT(thread_id) DO UPDATE SET
                title = excluded.title,
                state = excluded.state,
                created_at_utc = excluded.created_at_utc,
                updated_at_utc = excluded.updated_at_utc,
                payload_json = excluded.payload_json;
            """;
        command.Parameters.AddWithValue("$threadId", thread.ThreadId);
        command.Parameters.AddWithValue("$title", thread.Title);
        command.Parameters.AddWithValue("$state", thread.State.ToString());
        command.Parameters.AddWithValue("$createdAtUtc", thread.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updatedAtUtc", (thread.UpdatedAtUtc ?? thread.CreatedAtUtc).ToString("O"));
        command.Parameters.AddWithValue("$payloadJson", JsonSerializer.Serialize(thread, JsonOptions));
        command.ExecuteNonQuery();

        return Task.FromResult(thread);
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS conversation_threads (
                thread_id TEXT NOT NULL PRIMARY KEY,
                title TEXT NOT NULL,
                state TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                payload_json TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private static string ResolveDatabasePath(string databasePath)
    {
        var candidate = string.IsNullOrWhiteSpace(databasePath)
            ? ".appdata\\workbench.db"
            : databasePath.Trim();

        return Path.GetFullPath(candidate);
    }
}
