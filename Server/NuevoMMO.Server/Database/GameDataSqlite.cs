using System.Text.Json;
using Microsoft.Data.Sqlite;
using NuevoMMO.Core;

namespace NuevoMMO.Server.Database;

/// <summary>
/// Lectura de GameData (game.db) en el servidor. El esquema coincide con el del Editor:
/// tablas <c>definitions</c> + <c>content_meta</c>. El servidor no referencia al Editor.
/// </summary>
public static class GameDataSqlite
{
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

    public static bool FileExists(string path)
        => !string.IsNullOrWhiteSpace(path) && File.Exists(Path.GetFullPath(path));

    public static bool TryLoad(string path, out ContentPackage? package)
    {
        package = null;
        if (!FileExists(path)) return false;
        try
        {
            if (!HasDefinitionSchema(Path.GetFullPath(path))) return false;
            package = Load(path);
            return true;
        }
        catch (Exception exception) when (exception is InvalidDataException or SqliteException or JsonException or IOException)
        {
            return false;
        }
    }

    public static ContentPackage Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"No existe game.db: {fullPath}", fullPath);

        using var connection = Open(fullPath);
        if (!HasDefinitionSchema(connection))
            throw new InvalidDataException($"game.db no contiene el esquema de Definitions: {fullPath}");

        var packageVersion = ReadMeta(connection, "package_version") ?? "server";
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

        var package = new ContentPackage(
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
        package.ValidateOrThrow();
        return package;

        T[] Array<T>() where T : GameDefinition
            => loaded.TryGetValue(typeof(T), out var list) ? list.Cast<T>().ToArray() : [];
    }

    private static bool HasDefinitionSchema(string path)
    {
        using var connection = Open(path);
        return HasDefinitionSchema(connection);
    }

    private static bool HasDefinitionSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='definitions' LIMIT 1;";
        return command.ExecuteScalar() is not null;
    }

    private static SqliteConnection Open(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            Pooling = false
        }.ToString());
        connection.Open();
        return connection;
    }

    private static string? ReadMeta(SqliteConnection connection, string key)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT value FROM content_meta WHERE key = $key;
            """;
        try
        {
            command.Parameters.AddWithValue("$key", key);
            return command.ExecuteScalar() as string;
        }
        catch (SqliteException)
        {
            return null;
        }
    }
}
