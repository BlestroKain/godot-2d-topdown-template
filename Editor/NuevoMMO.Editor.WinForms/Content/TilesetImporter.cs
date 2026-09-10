using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class TilesetImporter
{
    private readonly DefinitionRegistry registry;
    private readonly TilesetImageProvider images;

    public TilesetImporter(DefinitionRegistry registry, TilesetImageProvider images)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.images = images ?? throw new ArgumentNullException(nameof(images));
    }

    public IReadOnlyList<TilesetDefinition> ImportClientTilesets(Vector2IntData tileSize)
    {
        if (tileSize.X <= 0 || tileSize.Y <= 0)
            throw new ArgumentException("TileSize debe ser positivo.", nameof(tileSize));

        var folder = images.ResolveTilesetFolder();
        var imported = new List<TilesetDefinition>();

        foreach (var path in Directory.EnumerateFiles(folder, "*.png", SearchOption.TopDirectoryOnly)
                     .Where(static path => !path.EndsWith(".png~", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var keyText = "tileset." + NormalizeKey(fileName);
            var key = new ContentKey(keyText);
            if (registry.TryGet<TilesetDefinition>(key, out _)) continue;

            var definition = new TilesetDefinition(
                DefinitionId.New(),
                key,
                fileName,
                $"Tileset importado desde Client/tilesets/{Path.GetFileName(path)}.",
                enabled: true,
                version: 1,
                tags: ["tileset", "imported"],
                textureKey: new ContentKey(NormalizeKey(fileName)),
                tileSize: tileSize);

            registry.Register(definition);
            imported.Add(definition);
        }

        return imported;
    }

    private static string NormalizeKey(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-'
                ? character
                : '-')
            .ToArray();
        return new string(chars).Trim('-');
    }
}
