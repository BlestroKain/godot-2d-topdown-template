using System.Text.Json;
using Microsoft.Data.Sqlite;
using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Persistencia de GameData en SQLite (game.db). El Editor y el servidor cargan Definitions
/// a RAM; este archivo es la fuente de verdad, no un JSON suelto.
/// </summary>
public static class GameDatabase
{
    public const string FileFilter = "NuevoMMO GameData (*.db)|*.db|Todos los archivos (*.*)|*.*";
    public const string DefaultFileName = "game.db";

    private static readonly Dictionary<string, Type> DefinitionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(MapDefinition)] = typeof(MapDefinition),
        [nameof(MobDefinition)] = typeof(MobDefinition),
        [nameof(ItemDefinition)] = typeof(ItemDefinition),
        [nameof(EffectDefinition)] = typeof(EffectDefinition),
        [nameof(TechniqueDefinition)] = typeof(TechniqueDefinition),
        [nameof(NpcDefinition)] = typeof(NpcDefinition),
        [nameof(ResourceDefinition)] = typeof(ResourceDefinition),
        [nameof(TraditionDefinition)] = typeof(TraditionDefinition),
        [nameof(ProfessionDefinition)] = typeof(ProfessionDefinition),
        [nameof(RecipeDefinition)] = typeof(RecipeDefinition),
        [nameof(LootTableDefinition)] = typeof(LootTableDefinition),
        [nameof(SpawnTableDefinition)] = typeof(SpawnTableDefinition),
        [nameof(DungeonDefinition)] = typeof(DungeonDefinition),
        [nameof(QuestDefinition)] = typeof(QuestDefinition),
        [nameof(ItemPropertyDefinition)] = typeof(ItemPropertyDefinition),
        [nameof(EventDefinition)] = typeof(EventDefinition),
        [nameof(TilesetDefinition)] = typeof(TilesetDefinition)
    };

    public static bool IsDatabasePath(string path)
        => Path.GetExtension(path).Equals(".db", StringComparison.OrdinalIgnoreCase);

    public static ContentPackage Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"No existe game.db: {fullPath}", fullPath);

        using var connection = Open(fullPath, create: false);

        var packageVersion = ReadMeta(connection, "package_version") ?? "editor";
        var formatText = ReadMeta(connection, "format_version");
        var formatVersion = int.TryParse(formatText, out var parsed) ? parsed : ContentPackage.CurrentFormatVersion;

        var loaded = new Dictionary<Type, List<GameDefinition>>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT type, json FROM definitions ORDER BY type, name;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var typeName = reader.GetString(0);
                var json = reader.GetString(1);
                if (!DefinitionTypes.TryGetValue(typeName, out var type))
                    throw new InvalidDataException($"Tipo de Definition desconocido en game.db: {typeName}.");

                var definition = JsonSerializer.Deserialize(json, type, ContentPackage.JsonOptions) as GameDefinition
                    ?? throw new InvalidDataException($"No se pudo leer {typeName} desde game.db.");
                if (!loaded.TryGetValue(type, out var list))
                {
                    list = [];
                    loaded[type] = list;
                }

                list.Add(definition);
            }
        }

        return new ContentPackage(
            formatVersion,
            packageVersion,
            Maps: Array<MapDefinition>(),
            Mobs: Array<MobDefinition>(),
            Items: Array<ItemDefinition>(),
            Effects: Array<EffectDefinition>(),
            Techniques: Array<TechniqueDefinition>(),
            Npcs: Array<NpcDefinition>(),
            Resources: Array<ResourceDefinition>(),
            Traditions: Array<TraditionDefinition>(),
            Professions: Array<ProfessionDefinition>(),
            Recipes: Array<RecipeDefinition>(),
            LootTables: Array<LootTableDefinition>(),
            SpawnTables: Array<SpawnTableDefinition>(),
            Dungeons: Array<DungeonDefinition>(),
            Quests: Array<QuestDefinition>(),
            ItemProperties: Array<ItemPropertyDefinition>())
        {
            Events = Array<EventDefinition>(),
            Tilesets = Array<TilesetDefinition>()
        };

        T[] Array<T>() where T : GameDefinition
            => loaded.TryGetValue(typeof(T), out var list) ? list.Cast<T>().ToArray() : [];
    }

    public static void Save(string path, ContentPackage package)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(package);
        package.ValidateOrThrow();

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory());

        using var connection = Open(fullPath, create: true);
        EnsureSchema(connection);
        using var transaction = connection.BeginTransaction();

        using (var clear = connection.CreateCommand())
        {
            clear.Transaction = transaction;
            clear.CommandText = "DELETE FROM definitions; DELETE FROM content_meta;";
            clear.ExecuteNonQuery();
        }

        WriteMeta(connection, transaction, "format_version", package.FormatVersion.ToString());
        WriteMeta(connection, transaction, "package_version", package.PackageVersion);

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO definitions(id, type, key, name, json) VALUES ($id, $type, $key, $name, $json);";
            var id = insert.CreateParameter(); id.ParameterName = "$id"; insert.Parameters.Add(id);
            var type = insert.CreateParameter(); type.ParameterName = "$type"; insert.Parameters.Add(type);
            var key = insert.CreateParameter(); key.ParameterName = "$key"; insert.Parameters.Add(key);
            var name = insert.CreateParameter(); name.ParameterName = "$name"; insert.Parameters.Add(name);
            var json = insert.CreateParameter(); json.ParameterName = "$json"; insert.Parameters.Add(json);

            foreach (var definition in package.All())
            {
                id.Value = definition.Id.ToString();
                type.Value = definition.GetType().Name;
                key.Value = definition.Key.Value;
                name.Value = definition.Name;
                json.Value = JsonSerializer.Serialize(definition, definition.GetType(), ContentPackage.JsonOptions);
                insert.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    private static SqliteConnection Open(string path, bool create)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = create ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            Pooling = false
        }.ToString());
        connection.Open();
        return connection;
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys=ON;
            CREATE TABLE IF NOT EXISTS content_meta (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS definitions (
                id TEXT PRIMARY KEY,
                type TEXT NOT NULL,
                key TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                json TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_definitions_type ON definitions(type);
            """;
        command.ExecuteNonQuery();
    }

    private static string? ReadMeta(SqliteConnection connection, string key)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM content_meta WHERE key = $key;";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() as string;
    }

    private static void WriteMeta(SqliteConnection connection, SqliteTransaction transaction, string key, string value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO content_meta(key, value) VALUES ($key, $value);";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }
}
