using System.Text.Json;
using Microsoft.Data.Sqlite;
using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Persistencia de GameData en SQLite (game.db), al patrón Intersect/Broken Reborn:
/// una tabla por tipo de contenido (Items, Maps, Events…) con identidad en columnas
/// y el grafo anidado en JSON. El Editor y el servidor cargan a RAM; no hay SQL por tick.
/// </summary>
public static class GameDatabase
{
    public const string FileFilter = "NuevoMMO GameData (*.db)|*.db|Todos los archivos (*.*)|*.*";
    public const string DefaultFileName = "game.db";

    internal static readonly Dictionary<Type, string> Tables = new()
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

    private static readonly Dictionary<string, Type> DefinitionTypes =
        Tables.ToDictionary(static pair => pair.Key.Name, static pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public static bool IsDatabasePath(string path)
        => Path.GetExtension(path).Equals(".db", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Ruta canónica de GameData compartida con el servidor: <c>Data/game.db</c>
    /// desde la raíz del repo. Crea la carpeta si no existe.
    /// </summary>
    public static string LocateSharedPath(string configured = "Data/" + DefaultFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configured);
        if (Path.IsPathRooted(configured))
        {
            var full = Path.GetFullPath(configured);
            EnsureDirectory(full);
            return full;
        }

        var relative = configured.Replace('/', Path.DirectorySeparatorChar);
        foreach (var origin in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var current = new DirectoryInfo(origin);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "NuevoMMO.sln")) ||
                    Directory.Exists(Path.Combine(current.FullName, "Server")))
                {
                    var path = Path.GetFullPath(Path.Combine(current.FullName, relative));
                    EnsureDirectory(path);
                    return path;
                }

                current = current.Parent;
            }
        }

        var fallback = Path.GetFullPath(relative);
        EnsureDirectory(fallback);
        return fallback;
    }

    private static void EnsureDirectory(string filePath)
    {
        var folder = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);
    }

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

        var loaded = LoadAll(connection);

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

        foreach (var table in Tables.Values)
        {
            using var clearType = connection.CreateCommand();
            clearType.Transaction = transaction;
            clearType.CommandText = $"DELETE FROM \"{table}\";";
            clearType.ExecuteNonQuery();
        }

        foreach (var definition in package.All())
            Insert(connection, transaction, definition);

        transaction.Commit();
    }

    /// <summary>Guarda una sola definición, como el Save de un FrmItem de Intersect.</summary>
    public static void Upsert(string path, GameDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(definition);
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory());
        using var connection = Open(fullPath, create: true);
        EnsureSchema(connection);
        using var transaction = connection.BeginTransaction();
        Delete(connection, transaction, definition);
        Insert(connection, transaction, definition);
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

        foreach (var table in Tables.Values)
        {
            using var typed = connection.CreateCommand();
            typed.CommandText = $"""
                CREATE TABLE IF NOT EXISTS "{table}" (
                    id TEXT PRIMARY KEY,
                    key TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL,
                    enabled INTEGER NOT NULL DEFAULT 1,
                    version INTEGER NOT NULL DEFAULT 1,
                    json TEXT NOT NULL
                );
                """;
            typed.ExecuteNonQuery();
        }
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

    private static void Insert(SqliteConnection connection, SqliteTransaction transaction, GameDefinition definition)
    {
        var json = JsonSerializer.Serialize(definition, definition.GetType(), ContentPackage.JsonOptions);
        using (var legacy = connection.CreateCommand())
        {
            legacy.Transaction = transaction;
            legacy.CommandText = "INSERT INTO definitions(id, type, key, name, json) VALUES ($id, $type, $key, $name, $json);";
            legacy.Parameters.AddWithValue("$id", definition.Id.ToString());
            legacy.Parameters.AddWithValue("$type", definition.GetType().Name);
            legacy.Parameters.AddWithValue("$key", definition.Key.Value);
            legacy.Parameters.AddWithValue("$name", definition.Name);
            legacy.Parameters.AddWithValue("$json", json);
            legacy.ExecuteNonQuery();
        }

        if (!Tables.TryGetValue(definition.GetType(), out var table)) return;
        using var typed = connection.CreateCommand();
        typed.Transaction = transaction;
        typed.CommandText = $"""
            INSERT INTO "{table}"(id, key, name, enabled, version, json)
            VALUES ($id, $key, $name, $enabled, $version, $json);
            """;
        typed.Parameters.AddWithValue("$id", definition.Id.ToString());
        typed.Parameters.AddWithValue("$key", definition.Key.Value);
        typed.Parameters.AddWithValue("$name", definition.Name);
        typed.Parameters.AddWithValue("$enabled", definition.Enabled ? 1 : 0);
        typed.Parameters.AddWithValue("$version", definition.Version);
        typed.Parameters.AddWithValue("$json", json);
        typed.ExecuteNonQuery();
    }

    private static void Delete(SqliteConnection connection, SqliteTransaction transaction, GameDefinition definition)
    {
        using (var legacy = connection.CreateCommand())
        {
            legacy.Transaction = transaction;
            legacy.CommandText = "DELETE FROM definitions WHERE id = $id;";
            legacy.Parameters.AddWithValue("$id", definition.Id.ToString());
            legacy.ExecuteNonQuery();
        }

        if (!Tables.TryGetValue(definition.GetType(), out var table)) return;
        using var typed = connection.CreateCommand();
        typed.Transaction = transaction;
        typed.CommandText = $"DELETE FROM \"{table}\" WHERE id = $id;";
        typed.Parameters.AddWithValue("$id", definition.Id.ToString());
        typed.ExecuteNonQuery();
    }

    private static bool TableExists(SqliteConnection connection, string name)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
        command.Parameters.AddWithValue("$name", name);
        return command.ExecuteScalar() is not null;
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
