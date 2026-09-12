using System.Text.Json;
using Microsoft.Data.Sqlite;
using NuevoMMO.Core;

namespace NuevoMMO.Server.Database;

/// <summary>
/// Lectura de GameData (game.db) en el servidor. El esquema coincide con el del Editor:
/// tablas tipadas (Items, Maps, Events…) más <c>definitions</c> legacy y <c>content_meta</c>.
/// El servidor no referencia al Editor.
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

        var loaded = LoadAll(connection);

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

    private static readonly Dictionary<Type, string> Tables = new()
    {
        [typeof(MapDefinition)] = "Maps",
        [typeof(MobDefinition)] = "Mobs",
        [typeof(ItemDefinition)] = "Items",
        [typeof(EffectDefinition)] = "Effects",
        [typeof(TechniqueDefinition)] = "Techniques",
        [typeof(NpcDefinition)] = "Npcs",
        [typeof(ResourceDefinition)] = "Resources",
        [typeof(TraditionDefinition)] = "Traditions",
        [typeof(ProfessionDefinition)] = "Professions",
        [typeof(RecipeDefinition)] = "Recipes",
        [typeof(LootTableDefinition)] = "LootTables",
        [typeof(SpawnTableDefinition)] = "SpawnTables",
        [typeof(DungeonDefinition)] = "Dungeons",
        [typeof(QuestDefinition)] = "Quests",
        [typeof(ItemPropertyDefinition)] = "ItemProperties",
        [typeof(EventDefinition)] = "Events",
        [typeof(TilesetDefinition)] = "Tilesets"
    };

    private static bool HasDefinitionSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name IN ('definitions','Items','Maps') LIMIT 1;";
        return command.ExecuteScalar() is not null;
    }

    private static Dictionary<Type, List<GameDefinition>> LoadAll(SqliteConnection connection)
    {
        var loaded = new Dictionary<Type, List<GameDefinition>>();
        var fromTyped = false;
        foreach (var (type, table) in Tables)
        {
            if (!TableExists(connection, table)) continue;
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT json FROM \"{table}\" ORDER BY name;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                fromTyped = true;
                Add(loaded, type, reader.GetString(0));
            }
        }

        if (fromTyped || !TableExists(connection, "definitions")) return loaded;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT type, json FROM definitions ORDER BY type, name;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var typeName = reader.GetString(0);
                if (!DefinitionTypes.TryGetValue(typeName, out var type))
                    throw new InvalidDataException($"Tipo de Definition desconocido en game.db: {typeName}.");
                Add(loaded, type, reader.GetString(1));
            }
        }

        return loaded;
    }

    private static void Add(Dictionary<Type, List<GameDefinition>> loaded, Type type, string json)
    {
        var definition = JsonSerializer.Deserialize(json, type, ContentPackage.JsonOptions) as GameDefinition
            ?? throw new InvalidDataException($"No se pudo leer {type.Name} desde game.db.");
        if (!loaded.TryGetValue(type, out var list))
        {
            list = [];
            loaded[type] = list;
        }

        if (list.Any(existing => existing.Id == definition.Id)) return;
        list.Add(definition);
    }

    private static bool TableExists(SqliteConnection connection, string name)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
        command.Parameters.AddWithValue("$name", name);
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
