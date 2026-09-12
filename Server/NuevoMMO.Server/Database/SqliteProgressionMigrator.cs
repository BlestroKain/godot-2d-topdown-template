using Microsoft.Data.Sqlite;

namespace NuevoMMO.Server.Database;

public static class SqliteProgressionMigrator
{
    private static readonly (string Name, string Definition)[] Columns =
    [
        ("available_attribute_points", "INTEGER NOT NULL DEFAULT 0"),
        ("strength", "INTEGER NOT NULL DEFAULT 10"),
        ("intelligence", "INTEGER NOT NULL DEFAULT 10"),
        ("agility", "INTEGER NOT NULL DEFAULT 10"),
        ("spirit", "INTEGER NOT NULL DEFAULT 10"),
        ("vitality", "INTEGER NOT NULL DEFAULT 10"),
        ("current_health", "INTEGER NULL"),
        ("current_mana", "INTEGER NULL"),
        ("tradition_id", "TEXT NOT NULL DEFAULT ''"),
        ("appearance_data", "TEXT NOT NULL DEFAULT 'v1|template.player||||||||'"),
        ("inventory_data", "TEXT NOT NULL DEFAULT '{\"items\":[],\"equipment\":[]}'")
    ];

    public static async Task ApplyAsync(string playersDatabasePath, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(SqliteMigrator.ConnectionString(playersDatabasePath));
        await connection.OpenAsync(cancellationToken);

        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var inspect = connection.CreateCommand())
        {
            inspect.CommandText = "PRAGMA table_info(characters)";
            await using var reader = await inspect.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                existing.Add(reader.GetString(1));
        }

        foreach (var column in Columns)
        {
            if (existing.Contains(column.Name)) continue;
            await using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE characters ADD COLUMN {column.Name} {column.Definition}";
            await alter.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var backfill = connection.CreateCommand();
        backfill.CommandText = """
            UPDATE characters
            SET available_attribute_points = MAX((level - 1) * 3, 0)
            WHERE available_attribute_points = 0
              AND strength = 10 AND intelligence = 10 AND agility = 10 AND spirit = 10 AND vitality = 10
            """;
        await backfill.ExecuteNonQueryAsync(cancellationToken);
    }
}
