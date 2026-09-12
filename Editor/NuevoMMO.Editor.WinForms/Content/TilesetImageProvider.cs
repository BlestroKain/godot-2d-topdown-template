using NuevoMMO.Core;
using System.Drawing;

namespace NuevoMMO.Editor;

/// <summary>
/// Resuelve TextureKey a imágenes reales del cliente sin contaminar Core con rutas ni System.Drawing.
/// </summary>
public sealed class TilesetImageProvider : IDisposable
{
    private static readonly string[] Extensions = [".png", ".webp", ".jpg", ".jpeg", ".bmp"];
    private readonly EditorConfiguration configuration;
    private readonly DefinitionRegistry registry;
    private readonly AssetLibrary? assets;
    private readonly Dictionary<ContentKey, Bitmap> cache = [];

    public TilesetImageProvider(EditorConfiguration configuration, DefinitionRegistry registry, AssetLibrary? assets = null)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.assets = assets;
    }

    public Bitmap Get(TilesetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (cache.TryGetValue(definition.TextureKey, out var cached)) return cached;

        var path = ResolvePath(definition.TextureKey);
        using var source = new Bitmap(path);
        var bitmap = new Bitmap(source);
        cache.Add(definition.TextureKey, bitmap);
        return bitmap;
    }

    public Bitmap Get(ContentKey tilesetKey)
        => Get(registry.Get<TilesetDefinition>(tilesetKey));

    public bool TryGet(ContentKey tilesetKey, out Bitmap? bitmap)
    {
        bitmap = null;
        if (!registry.TryGet<TilesetDefinition>(tilesetKey, out var definition) || definition is null)
            return false;

        try
        {
            bitmap = Get(definition);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public string ResolvePath(ContentKey textureKey)
    {
        if (assets is not null && assets.TryResolve(textureKey, out var fromLibrary))
            return fromLibrary;

        var tilesetFolder = ResolveTilesetFolder();
        var key = textureKey.Value;
        var stem = AssetCatalog.TryKind(textureKey, out _)
            ? AssetCatalog.FileStem(textureKey)
            : key.StartsWith("tileset.", StringComparison.OrdinalIgnoreCase)
                ? key[8..]
                : key;

        var candidates = new List<string>();
        AddCandidates(candidates, tilesetFolder, key);
        if (!string.Equals(stem, key, StringComparison.OrdinalIgnoreCase))
            AddCandidates(candidates, tilesetFolder, stem);

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);

        throw new FileNotFoundException(
            $"No se encontró la textura '{textureKey}' en '{tilesetFolder}'. Candidatos: {string.Join(", ", candidates)}");
    }

    public string ResolveTilesetFolder()
    {
        if (assets is not null)
            return assets.Folder(AssetKind.Tileset);

        var shared = Path.Combine(
            AssetLibrary.LocateSharedRoot(configuration.ResourcesRoot),
            AssetCatalog.Folder(AssetKind.Tileset));
        Directory.CreateDirectory(shared);
        return Path.GetFullPath(shared);
    }

    public void Clear()
    {
        foreach (var bitmap in cache.Values) bitmap.Dispose();
        cache.Clear();
    }

    public void Dispose() => Clear();

    private static void AddCandidates(ICollection<string> candidates, string root, string value)
    {
        var normalized = value.Replace('/', Path.DirectorySeparatorChar);
        candidates.Add(Path.Combine(root, normalized));

        if (Path.HasExtension(normalized)) return;
        foreach (var extension in Extensions)
            candidates.Add(Path.Combine(root, normalized + extension));

        var dottedAsFolders = normalized.Replace('.', Path.DirectorySeparatorChar);
        if (!string.Equals(dottedAsFolders, normalized, StringComparison.Ordinal))
        {
            foreach (var extension in Extensions)
                candidates.Add(Path.Combine(root, dottedAsFolders + extension));
        }
    }
}
