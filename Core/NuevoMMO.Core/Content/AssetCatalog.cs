namespace NuevoMMO.Core;

/// <summary>
/// Catálogo de rutas de assets, al patrón Intersect <c>resources/{tipo}/{archivo}.png</c>.
/// Core no abre archivos: solo traduce ContentKey ↔ carpeta/archivo.
/// El Editor y el cliente resuelven contra disco o <c>res://</c>.
/// </summary>
public enum AssetKind : byte
{
    Tileset,
    Item,
    Entity,
    Spell,
    Animation,
    Face,
    Image,
    Fog,
    Resource,
    Paperdoll,
    Gui,
    Misc,
    Sound,
    Music
}

public static class AssetCatalog
{
    /// <summary>Nombre de la carpeta de assets (Godot: <c>res://resources</c>).</summary>
    public const string DefaultRoot = "resources";

    /// <summary>
    /// Única raíz compartida desde el repo: editor, servidor y Godot leen aquí.
    /// Godot la ve como <c>res://resources</c> porque el proyecto está en <c>Client/</c>.
    /// </summary>
    public const string SharedRootFromRepo = "Client/resources";

    public static readonly IReadOnlyList<AssetKind> AllKinds = Enum.GetValues<AssetKind>();

    public static string Folder(AssetKind kind) => kind switch
    {
        AssetKind.Tileset => "tilesets",
        AssetKind.Item => "items",
        AssetKind.Entity => "entities",
        AssetKind.Spell => "spells",
        AssetKind.Animation => "animations",
        AssetKind.Face => "faces",
        AssetKind.Image => "images",
        AssetKind.Fog => "fogs",
        AssetKind.Resource => "resources",
        AssetKind.Paperdoll => "paperdolls",
        AssetKind.Gui => "gui",
        AssetKind.Misc => "misc",
        AssetKind.Sound => "sounds",
        AssetKind.Music => "music",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static string RelativePath(AssetKind kind, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var name = Path.GetFileName(fileName.Trim().Replace('\\', '/'));
        if (!Path.HasExtension(name))
            name += kind is AssetKind.Sound ? ".wav" : kind is AssetKind.Music ? ".ogg" : ".png";
        return $"{Folder(kind)}/{name}";
    }

    public static string RelativePath(ContentKey key)
    {
        if (!TryKind(key, out var kind))
            return key.Value.Replace('.', '/') + ".png";
        return RelativePath(kind, FileStem(key));
    }

    public static ContentKey Key(AssetKind kind, string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName.Trim());
        if (string.IsNullOrWhiteSpace(stem)) throw new ArgumentException("Nombre de archivo vacío.", nameof(fileName));
        return new ContentKey($"{Folder(kind)}.{Normalize(stem)}");
    }

    public static string FileStem(ContentKey key)
    {
        if (key.IsEmpty) return string.Empty;
        var value = key.Value;
        var dot = value.LastIndexOf('.');
        return dot < 0 ? Normalize(value) : Normalize(value[(dot + 1)..]);
    }

    public static bool TryKind(ContentKey key, out AssetKind kind)
    {
        kind = default;
        if (key.IsEmpty) return false;
        var value = key.Value;
        foreach (var candidate in AllKinds)
        {
            var folder = Folder(candidate);
            if (value.StartsWith(folder + ".", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
            {
                kind = candidate;
                return true;
            }
        }

        if (value.StartsWith("tileset.", StringComparison.OrdinalIgnoreCase)) { kind = AssetKind.Tileset; return true; }
        if (value.StartsWith("visuals.item", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("items.", StringComparison.OrdinalIgnoreCase)) { kind = AssetKind.Item; return true; }
        if (value.StartsWith("visuals.technique", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("techniques.", StringComparison.OrdinalIgnoreCase)) { kind = AssetKind.Spell; return true; }
        if (value.StartsWith("visuals.effect", StringComparison.OrdinalIgnoreCase)) { kind = AssetKind.Animation; return true; }
        if (value.StartsWith("visuals.resource", StringComparison.OrdinalIgnoreCase)) { kind = AssetKind.Resource; return true; }
        if (value.StartsWith("visuals.", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("template.", StringComparison.OrdinalIgnoreCase)) { kind = AssetKind.Entity; return true; }
        return false;
    }

    public static AssetKind KindForDefinition(Type definitionType)
    {
        ArgumentNullException.ThrowIfNull(definitionType);
        if (definitionType == typeof(TilesetDefinition)) return AssetKind.Tileset;
        if (definitionType == typeof(ItemDefinition)) return AssetKind.Item;
        if (definitionType == typeof(TechniqueDefinition)) return AssetKind.Spell;
        if (definitionType == typeof(EffectDefinition)) return AssetKind.Animation;
        if (definitionType == typeof(ResourceDefinition)) return AssetKind.Resource;
        if (definitionType == typeof(MobDefinition) || definitionType == typeof(NpcDefinition) ||
            definitionType == typeof(TraditionDefinition))
            return AssetKind.Entity;
        return AssetKind.Misc;
    }

    public static string GodotPath(ContentKey key, string root = DefaultRoot)
        => $"res://{root}/{RelativePath(key)}".Replace('\\', '/');

    public static string DiskPath(string sharedRoot, ContentKey key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sharedRoot);
        return Path.Combine(sharedRoot, RelativePath(key).Replace('/', Path.DirectorySeparatorChar));
    }

    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var chars = value.Trim().ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-'
                ? character
                : '-')
            .ToArray();
        return new string(chars).Trim('-');
    }
}
