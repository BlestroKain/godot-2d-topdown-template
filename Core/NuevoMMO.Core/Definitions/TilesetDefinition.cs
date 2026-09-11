namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de un tileset. El servidor solo necesita identidad/configuración;
/// Editor y Client resuelven TextureKey al recurso gráfico real.
/// </summary>
public sealed record TilesetDefinition : GameDefinition
{
    public TilesetDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey textureKey,
        Vector2IntData tileSize,
        int autotileAnimationFrames = 3,
        int autotileFrameMilliseconds = 600,
        int waterfallAnimationFrames = 3,
        int waterfallFrameMilliseconds = 500,
        Dictionary<string, string>? metadata = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (textureKey.IsEmpty) throw new ArgumentException("TextureKey vacío.", nameof(textureKey));
        if (tileSize.X <= 0 || tileSize.Y <= 0) throw new ArgumentException("TileSize debe ser positivo.", nameof(tileSize));
        if ((tileSize.X & 1) != 0 || (tileSize.Y & 1) != 0)
            throw new ArgumentException("TileSize debe ser par para resolver quarter-tiles.", nameof(tileSize));
        if (autotileAnimationFrames < 1) throw new ArgumentOutOfRangeException(nameof(autotileAnimationFrames));
        if (autotileFrameMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(autotileFrameMilliseconds));
        if (waterfallAnimationFrames < 1) throw new ArgumentOutOfRangeException(nameof(waterfallAnimationFrames));
        if (waterfallFrameMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(waterfallFrameMilliseconds));

        TextureKey = textureKey;
        TileSize = tileSize;
        AutotileAnimationFrames = autotileAnimationFrames;
        AutotileFrameMilliseconds = autotileFrameMilliseconds;
        WaterfallAnimationFrames = waterfallAnimationFrames;
        WaterfallFrameMilliseconds = waterfallFrameMilliseconds;
        Metadata = DefinitionCollectionGuards.CopyText(metadata, nameof(metadata));
    }

    public static bool IsAtlasSizeCompatible(int atlasWidth, int atlasHeight, Vector2IntData tileSize)
        => atlasWidth > 0 &&
           atlasHeight > 0 &&
           tileSize.X > 0 &&
           tileSize.Y > 0 &&
           atlasWidth % tileSize.X == 0 &&
           atlasHeight % tileSize.Y == 0;

    public ContentKey TextureKey { get; }
    public Vector2IntData TileSize { get; }
    public int AutotileAnimationFrames { get; }
    public int AutotileFrameMilliseconds { get; }
    public int WaterfallAnimationFrames { get; }
    public int WaterfallFrameMilliseconds { get; }
    public Dictionary<string, string> Metadata { get; }
}
