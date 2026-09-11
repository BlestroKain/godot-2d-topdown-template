using Npgsql;
using NuevoMMO.Core;

namespace NuevoMMO.Server.Database;

public sealed class PostgresAccountRepository(string connectionString) : IAccountRepository
{
    public async Task<AccountRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id, username, password_hash, created_at FROM accounts WHERE lower(username) = lower(@username) LIMIT 1", connection);
        command.Parameters.AddWithValue("username", username);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new AccountRecord
        {
            Id = new(reader.GetGuid(0)),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(3)
        };
    }

    public async Task<AccountRecord> CreateAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
    {
        var record = new AccountRecord
        {
            Id = new(Guid.NewGuid()), Username = username,
            PasswordHash = passwordHash, CreatedAt = DateTimeOffset.UtcNow
        };
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO accounts (id, username, password_hash, created_at) VALUES (@id, @username, @hash, @created)", connection);
        command.Parameters.AddWithValue("id", record.Id.Value);
        command.Parameters.AddWithValue("username", record.Username);
        command.Parameters.AddWithValue("hash", record.PasswordHash);
        command.Parameters.AddWithValue("created", record.CreatedAt);
        try { await command.ExecuteNonQueryAsync(cancellationToken); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        { throw new InvalidOperationException("El usuario ya existe.", exception); }
        return record;
    }
}

public sealed class PostgresSessionRepository(string connectionString) : ISessionRepository
{
    public async Task<SessionRecord> CreateAsync(AccountId account, string token, CancellationToken cancellationToken = default)
    {
        var record = new SessionRecord
        {
            Id = new(Guid.NewGuid()), AccountId = account, Token = token,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO sessions (id, account_id, token, created_at, revoked_at) VALUES (@id, @account, @token, @created, NULL)", connection);
        command.Parameters.AddWithValue("id", record.Id.Value);
        command.Parameters.AddWithValue("account", record.AccountId.Value);
        command.Parameters.AddWithValue("token", record.Token);
        command.Parameters.AddWithValue("created", record.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return record;
    }

    public async Task<SessionRecord?> GetActiveAsync(SessionId id, string token, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id, account_id, token, created_at, revoked_at FROM sessions WHERE id = @id AND token = @token AND revoked_at IS NULL LIMIT 1", connection);
        command.Parameters.AddWithValue("id", id.Value);
        command.Parameters.AddWithValue("token", token);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new SessionRecord
        {
            Id = new(reader.GetGuid(0)), AccountId = new(reader.GetGuid(1)), Token = reader.GetString(2),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(3),
            RevokedAt = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4)
        };
    }

    public async Task RevokeAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "UPDATE sessions SET revoked_at = COALESCE(revoked_at, @now) WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id.Value);
        command.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class PostgresCharacterRepository(string connectionString) : ICharacterRepository
{
    private const string CharacterColumns =
        "id, account_id, name, map_definition, position_x, position_y, level, experience, " +
        "available_attribute_points, strength, intelligence, agility, spirit, vitality, current_health, current_mana, tradition_id";

    public async Task<IReadOnlyList<CharacterRecord>> ListByAccountAsync(AccountId account, CancellationToken cancellationToken = default)
    {
        var result = new List<CharacterRecord>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT {CharacterColumns} FROM characters WHERE account_id = @account ORDER BY name", connection);
        command.Parameters.AddWithValue("account", account.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadCharacter(reader));
        return result;
    }

    public async Task<CharacterRecord?> GetAsync(CharacterId id, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT {CharacterColumns} FROM characters WHERE id = @id LIMIT 1", connection);
        command.Parameters.AddWithValue("id", id.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadCharacter(reader) : null;
    }

    public async Task<CharacterRecord> CreateAsync(
        AccountId account,
        string name,
        DefinitionId map,
        Vector2Data position,
        DefinitionId traditionId,
        CancellationToken cancellationToken = default)
    {
        var record = new CharacterRecord
        {
            Id = new(Guid.NewGuid()), AccountId = account, Name = name,
            MapDefinition = map, Position = position, TraditionId = traditionId
        };
        record.ApplyProgression(ProgressionRules.CreateInitial());

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO characters (
                id, account_id, name, map_definition, position_x, position_y,
                level, experience, available_attribute_points,
                strength, intelligence, agility, spirit, vitality, current_health, current_mana, tradition_id)
            VALUES (
                @id, @account, @name, @map, @x, @y,
                @level, @experience, @points,
                @str, @int, @agi, @spi, @vit, NULL, NULL, @tradition)
            """, connection);
        AddIdentityParameters(command, record);
        AddProgressionParameters(command, record.ToProgressionState());
        try { await command.ExecuteNonQueryAsync(cancellationToken); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        { throw new InvalidOperationException("El nombre de personaje ya existe.", exception); }
        return record;
    }

    public async Task SavePositionAsync(CharacterId id, DefinitionId map, Vector2Data position, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "UPDATE characters SET map_definition = @map, position_x = @x, position_y = @y WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id.Value);
        command.Parameters.AddWithValue("map", map.Value);
        command.Parameters.AddWithValue("x", position.X);
        command.Parameters.AddWithValue("y", position.Y);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new KeyNotFoundException("Personaje inexistente.");
    }

    public async Task SaveCheckpointAsync(
        CharacterId id,
        DefinitionId map,
        Vector2Data position,
        PlayerProgressionState progression,
        int currentHealth,
        int currentMana,
        CancellationToken cancellationToken = default)
    {
        ProgressionRules.Validate(progression);
        if (currentHealth < 0 || currentMana < 0) throw new ArgumentOutOfRangeException(nameof(currentHealth));

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE characters SET
                map_definition = @map, position_x = @x, position_y = @y,
                level = @level, experience = @experience, available_attribute_points = @points,
                strength = @str, intelligence = @int, agility = @agi, spirit = @spi, vitality = @vit,
                current_health = @health, current_mana = @mana
            WHERE id = @id
            """, connection);
        command.Parameters.AddWithValue("id", id.Value);
        command.Parameters.AddWithValue("map", map.Value);
        command.Parameters.AddWithValue("x", position.X);
        command.Parameters.AddWithValue("y", position.Y);
        AddProgressionParameters(command, progression);
        command.Parameters.AddWithValue("health", currentHealth);
        command.Parameters.AddWithValue("mana", currentMana);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new KeyNotFoundException("Personaje inexistente.");
    }

    private static void AddIdentityParameters(NpgsqlCommand command, CharacterRecord record)
    {
        command.Parameters.AddWithValue("id", record.Id.Value);
        command.Parameters.AddWithValue("account", record.AccountId.Value);
        command.Parameters.AddWithValue("name", record.Name);
        command.Parameters.AddWithValue("map", record.MapDefinition.Value);
        command.Parameters.AddWithValue("x", record.Position.X);
        command.Parameters.AddWithValue("y", record.Position.Y);
        command.Parameters.AddWithValue("tradition", record.TraditionId.Value);
    }

    private static void AddProgressionParameters(NpgsqlCommand command, PlayerProgressionState progression)
    {
        command.Parameters.AddWithValue("level", progression.Level);
        command.Parameters.AddWithValue("experience", progression.Experience);
        command.Parameters.AddWithValue("points", progression.AvailableAttributePoints);
        command.Parameters.AddWithValue("str", progression.NaturalAttributes.Strength);
        command.Parameters.AddWithValue("int", progression.NaturalAttributes.Intelligence);
        command.Parameters.AddWithValue("agi", progression.NaturalAttributes.Agility);
        command.Parameters.AddWithValue("spi", progression.NaturalAttributes.Spirit);
        command.Parameters.AddWithValue("vit", progression.NaturalAttributes.Vitality);
    }

    private static CharacterRecord ReadCharacter(NpgsqlDataReader reader) => new()
    {
        Id = new(reader.GetGuid(0)), AccountId = new(reader.GetGuid(1)), Name = reader.GetString(2),
        MapDefinition = new(reader.GetGuid(3)), Position = new(reader.GetFloat(4), reader.GetFloat(5)),
        Level = reader.GetInt32(6), Experience = reader.GetInt64(7), AvailableAttributePoints = reader.GetInt32(8),
        Strength = reader.GetInt32(9), Intelligence = reader.GetInt32(10), Agility = reader.GetInt32(11),
        Spirit = reader.GetInt32(12), Vitality = reader.GetInt32(13),
        CurrentHealth = reader.IsDBNull(14) ? null : reader.GetInt32(14),
        CurrentMana = reader.IsDBNull(15) ? null : reader.GetInt32(15),
        TraditionId = reader.IsDBNull(16) ? DefinitionId.Empty : new(reader.GetGuid(16))
    };
}

public static class PostgresMigrator
{
    public static async Task ApplyAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var assembly = typeof(PostgresMigrator).Assembly;
        var migrations = assembly.GetManifestResourceNames()
            .Where(name => name.Contains("Database.Migrations.", StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        foreach (var resource in migrations)
        {
            await using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Migración embebida no encontrada: {resource}");
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
