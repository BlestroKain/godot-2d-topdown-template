using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using NuevoMMO.Core;
using NuevoMMO.Server.Configuration;

namespace NuevoMMO.Server.Database;

public sealed record SqliteDatabasePaths(string Auth, string Players, string Game, string Logs)
{
    public static SqliteDatabasePaths FromConfiguration(SqliteDatabaseFiles files)
    {
        ArgumentNullException.ThrowIfNull(files);
        return new(
            Resolve(files.Auth), Resolve(files.Players), Resolve(files.Game), Resolve(files.Logs));
    }

    private static string Resolve(string path)
    {
        var resolved = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        return Path.GetFullPath(resolved);
    }
}

public static class SqliteMigrator
{
    public static async Task ApplyAsync(SqliteDatabasePaths paths, CancellationToken cancellationToken = default)
    {
        foreach (var path in new[] { paths.Auth, paths.Players, paths.Game, paths.Logs })
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        }

        await ExecuteAsync(paths.Auth, AuthSchema, cancellationToken);
        await ExecuteAsync(paths.Players, PlayersSchema, cancellationToken);
        await ExecuteAsync(paths.Game, GameSchema, cancellationToken);
        await ExecuteAsync(paths.Logs, LogsSchema, cancellationToken);
    }

    private static async Task ExecuteAsync(string path, string sql, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(ConnectionString(path));
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static string ConnectionString(string path)
        => new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true
        }.ToString();

    private const string AuthSchema = """
        PRAGMA journal_mode=WAL;
        PRAGMA foreign_keys=ON;
        CREATE TABLE IF NOT EXISTS schema_info (version INTEGER NOT NULL);
        INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM schema_info);
        CREATE TABLE IF NOT EXISTS accounts (
            id TEXT PRIMARY KEY,
            username TEXT NOT NULL COLLATE NOCASE UNIQUE,
            password_hash TEXT NOT NULL,
            created_at TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS sessions (
            id TEXT PRIMARY KEY,
            account_id TEXT NOT NULL,
            token TEXT NOT NULL,
            created_at TEXT NOT NULL,
            revoked_at TEXT NULL,
            FOREIGN KEY(account_id) REFERENCES accounts(id) ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS ix_sessions_account_id ON sessions(account_id);
        CREATE INDEX IF NOT EXISTS ix_sessions_active ON sessions(account_id, revoked_at);
        """;

    private const string PlayersSchema = """
        PRAGMA journal_mode=WAL;
        PRAGMA foreign_keys=ON;
        CREATE TABLE IF NOT EXISTS schema_info (version INTEGER NOT NULL);
        INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM schema_info);
        CREATE TABLE IF NOT EXISTS characters (
            id TEXT PRIMARY KEY,
            account_id TEXT NOT NULL,
            name TEXT NOT NULL COLLATE NOCASE UNIQUE,
            map_definition TEXT NOT NULL,
            position_x REAL NOT NULL,
            position_y REAL NOT NULL,
            level INTEGER NOT NULL DEFAULT 1,
            experience INTEGER NOT NULL DEFAULT 0
        );
        CREATE INDEX IF NOT EXISTS ix_characters_account_id ON characters(account_id);
        """;

    private const string GameSchema = """
        PRAGMA journal_mode=WAL;
        CREATE TABLE IF NOT EXISTS schema_info (version INTEGER NOT NULL);
        INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM schema_info);
        CREATE TABLE IF NOT EXISTS content_packages (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            package_version TEXT NOT NULL UNIQUE,
            payload_json TEXT NOT NULL,
            created_at TEXT NOT NULL
        );
        """;

    private const string LogsSchema = """
        PRAGMA journal_mode=WAL;
        CREATE TABLE IF NOT EXISTS schema_info (version INTEGER NOT NULL);
        INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM schema_info);
        CREATE TABLE IF NOT EXISTS server_log_entries (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            created_at TEXT NOT NULL,
            level TEXT NOT NULL,
            category TEXT NOT NULL,
            message TEXT NOT NULL,
            data_json TEXT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_server_log_entries_created_at ON server_log_entries(created_at);
        """;
}

public sealed class SqliteAccountRepository(string databasePath) : IAccountRepository
{
    private readonly string connectionString = SqliteMigrator.ConnectionString(databasePath);

    public async Task<AccountRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, username, password_hash, created_at FROM accounts WHERE username = $username LIMIT 1";
        command.Parameters.AddWithValue("$username", username);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new AccountRecord
        {
            Id = new(Guid.Parse(reader.GetString(0))),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(3))
        };
    }

    public async Task<AccountRecord> CreateAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
    {
        var record = new AccountRecord
        {
            Id = new(Guid.NewGuid()), Username = username, PasswordHash = passwordHash, CreatedAt = DateTimeOffset.UtcNow
        };
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO accounts(id, username, password_hash, created_at) VALUES($id, $username, $password, $created)";
        command.Parameters.AddWithValue("$id", record.Id.Value.ToString("D"));
        command.Parameters.AddWithValue("$username", record.Username);
        command.Parameters.AddWithValue("$password", record.PasswordHash);
        command.Parameters.AddWithValue("$created", record.CreatedAt.ToString("O"));
        try { await command.ExecuteNonQueryAsync(cancellationToken); }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        { throw new InvalidOperationException("Usuario duplicado.", exception); }
        return record;
    }
}

public sealed class SqliteSessionRepository(string databasePath) : ISessionRepository
{
    private readonly string connectionString = SqliteMigrator.ConnectionString(databasePath);

    public async Task<SessionRecord> CreateAsync(AccountId account, string token, CancellationToken cancellationToken = default)
    {
        var record = new SessionRecord
        {
            Id = new(Guid.NewGuid()), AccountId = account, Token = token, CreatedAt = DateTimeOffset.UtcNow
        };
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO sessions(id, account_id, token, created_at, revoked_at) VALUES($id, $account, $token, $created, NULL)";
        command.Parameters.AddWithValue("$id", record.Id.Value.ToString("D"));
        command.Parameters.AddWithValue("$account", record.AccountId.Value.ToString("D"));
        command.Parameters.AddWithValue("$token", record.Token);
        command.Parameters.AddWithValue("$created", record.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return record;
    }

    public async Task<SessionRecord?> GetActiveAsync(SessionId id, string token, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, account_id, token, created_at, revoked_at FROM sessions WHERE id = $id AND revoked_at IS NULL LIMIT 1";
        command.Parameters.AddWithValue("$id", id.Value.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var stored = reader.GetString(2);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(stored), Encoding.UTF8.GetBytes(token))) return null;
        return new SessionRecord
        {
            Id = new(Guid.Parse(reader.GetString(0))), AccountId = new(Guid.Parse(reader.GetString(1))), Token = stored,
            CreatedAt = DateTimeOffset.Parse(reader.GetString(3)),
            RevokedAt = reader.IsDBNull(4) ? null : DateTimeOffset.Parse(reader.GetString(4))
        };
    }

    public async Task RevokeAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE sessions SET revoked_at = $revoked WHERE id = $id AND revoked_at IS NULL";
        command.Parameters.AddWithValue("$revoked", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$id", id.Value.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class SqliteCharacterRepository(string databasePath) : ICharacterRepository
{
    private readonly string connectionString = SqliteMigrator.ConnectionString(databasePath);

    public async Task<IReadOnlyList<CharacterRecord>> ListByAccountAsync(AccountId account, CancellationToken cancellationToken = default)
    {
        var result = new List<CharacterRecord>();
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, account_id, name, map_definition, position_x, position_y, level, experience FROM characters WHERE account_id = $account ORDER BY name";
        command.Parameters.AddWithValue("$account", account.Value.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadCharacter(reader));
        return result;
    }

    public async Task<CharacterRecord?> GetAsync(CharacterId id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, account_id, name, map_definition, position_x, position_y, level, experience FROM characters WHERE id = $id LIMIT 1";
        command.Parameters.AddWithValue("$id", id.Value.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadCharacter(reader) : null;
    }

    public async Task<CharacterRecord> CreateAsync(AccountId account, string name, DefinitionId map, Vector2Data position, CancellationToken cancellationToken = default)
    {
        var record = new CharacterRecord
        {
            Id = new(Guid.NewGuid()), AccountId = account, Name = name, MapDefinition = map, Position = position
        };
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO characters(id, account_id, name, map_definition, position_x, position_y, level, experience) VALUES($id, $account, $name, $map, $x, $y, $level, $experience)";
        command.Parameters.AddWithValue("$id", record.Id.Value.ToString("D"));
        command.Parameters.AddWithValue("$account", record.AccountId.Value.ToString("D"));
        command.Parameters.AddWithValue("$name", record.Name);
        command.Parameters.AddWithValue("$map", record.MapDefinition.Value.ToString("D"));
        command.Parameters.AddWithValue("$x", record.Position.X);
        command.Parameters.AddWithValue("$y", record.Position.Y);
        command.Parameters.AddWithValue("$level", record.Level);
        command.Parameters.AddWithValue("$experience", record.Experience);
        try { await command.ExecuteNonQueryAsync(cancellationToken); }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        { throw new InvalidOperationException("Nombre de personaje duplicado.", exception); }
        return record;
    }

    public async Task SavePositionAsync(CharacterId id, DefinitionId map, Vector2Data position, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE characters SET map_definition = $map, position_x = $x, position_y = $y WHERE id = $id";
        command.Parameters.AddWithValue("$map", map.Value.ToString("D"));
        command.Parameters.AddWithValue("$x", position.X);
        command.Parameters.AddWithValue("$y", position.Y);
        command.Parameters.AddWithValue("$id", id.Value.ToString("D"));
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            throw new KeyNotFoundException("Personaje inexistente.");
    }

    private static CharacterRecord ReadCharacter(SqliteDataReader reader) => new()
    {
        Id = new(Guid.Parse(reader.GetString(0))), AccountId = new(Guid.Parse(reader.GetString(1))), Name = reader.GetString(2),
        MapDefinition = new(Guid.Parse(reader.GetString(3))), Position = new(reader.GetFloat(4), reader.GetFloat(5)),
        Level = reader.GetInt32(6), Experience = reader.GetInt64(7)
    };
}
