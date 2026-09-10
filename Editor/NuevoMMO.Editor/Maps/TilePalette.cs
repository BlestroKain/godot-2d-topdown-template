using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class TilePalette
{
    public ContentKey? SelectedTilesetKey { get; private set; }
    public Vector2IntData SelectedAtlasCell { get; private set; }
    public int Alternative { get; private set; }
    public int RotationQuarterTurns { get; private set; }
    public bool FlipHorizontal { get; private set; }
    public bool FlipVertical { get; private set; }
    public MapAutotileMode AutotileMode { get; private set; }

    public bool HasSelection => SelectedTilesetKey is not null;

    public void Select(
        ContentKey tilesetKey,
        Vector2IntData atlasCell,
        int alternative = 0,
        int rotationQuarterTurns = 0,
        bool flipHorizontal = false,
        bool flipVertical = false,
        MapAutotileMode autotileMode = MapAutotileMode.None)
    {
        if (tilesetKey.IsEmpty) throw new ArgumentException("TilesetKey vacío.", nameof(tilesetKey));
        if (alternative < 0) throw new ArgumentOutOfRangeException(nameof(alternative));
        if (rotationQuarterTurns is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(rotationQuarterTurns));

        SelectedTilesetKey = tilesetKey;
        SelectedAtlasCell = atlasCell;
        Alternative = alternative;
        RotationQuarterTurns = rotationQuarterTurns;
        FlipHorizontal = flipHorizontal;
        FlipVertical = flipVertical;
        AutotileMode = autotileMode;
    }

    public void SetAutotileMode(MapAutotileMode mode) => AutotileMode = mode;

    public void Clear()
    {
        SelectedTilesetKey = null;
        AutotileMode = MapAutotileMode.None;
    }

    public MapTilePlacementDefinition CreatePlacement(Vector2IntData cell)
    {
        var tileset = SelectedTilesetKey ?? throw new InvalidOperationException("No hay un tile seleccionado.");
        return new MapTilePlacementDefinition(
            cell,
            tileset,
            SelectedAtlasCell,
            Alternative,
            RotationQuarterTurns,
            FlipHorizontal,
            FlipVertical,
            AutotileMode);
    }
}
