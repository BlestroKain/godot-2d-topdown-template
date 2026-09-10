using System.Drawing;
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
        var candidates = Directory.EnumerateFiles(folder, "*.png", SearchOption.TopDirectoryOnly)
            .Where(static path => !path.EndsWith(".png~", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path =>
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                return (path, fileName, key: new ContentKey("tileset." + NormalizeKey(fileName)));
            })
            .Where(candidate => !registry.TryGet<TilesetDefinition>(candidate.key, out _))
            .ToArray();

        var invalid = new List<string>();
        foreach (var candidate in candidates)
        {
            using var atlas = new Bitmap(candidate.path);
            if (!TilesetDefinition.IsAtlasSizeCompatible(atlas.Width, atlas.Height, tileSize))
                invalid.Add($"{Path.GetFileName(candidate.path)} ({atlas.Width}x{atlas.Height})");
        }

        if (invalid.Count > 0)
            throw new InvalidOperationException(
                $"Hay atlas cuyo tamaño no es múltiplo de {tileSize.X}x{tileSize.Y}: {string.Join(", ", invalid)}.");

        var imported = new List<TilesetDefinition>();
        foreach (var candidate in candidates)
        {
            var definition = new TilesetDefinition(
                DefinitionId.New(),
                candidate.key,
                candidate.fileName,
                $"Tileset importado desde Client/tilesets/{Path.GetFileName(candidate.path)}.",
                enabled: true,
                version: 1,
                tags: ["tileset", "imported"],
                textureKey: new ContentKey(NormalizeKey(candidate.fileName)),
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
