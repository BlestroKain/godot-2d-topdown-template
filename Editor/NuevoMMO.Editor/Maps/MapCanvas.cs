using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class MapCanvas
{
    public MapDocument? Document { get; private set; }
    public float Zoom { get; private set; } = 1f;
    public Vector2Data Pan { get; private set; } = Vector2Data.Zero;
    public bool GridVisible { get; set; } = true;
    public bool SnapToGrid { get; set; } = true;

    public void Bind(MapDocument document) => Document = document ?? throw new ArgumentNullException(nameof(document));
    public void Clear() => Document = null;

    public void SetZoom(float zoom)
    {
        if (!float.IsFinite(zoom) || zoom is < 0.1f or > 16f) throw new ArgumentOutOfRangeException(nameof(zoom));
        Zoom = zoom;
    }

    public void SetPan(Vector2Data pan)
    {
        if (!pan.IsFinite) throw new ArgumentException("Pan no finito.", nameof(pan));
        Pan = pan;
    }

    public Vector2IntData WorldToCell(Vector2Data world)
    {
        var document = Document ?? throw new InvalidOperationException("No hay mapa abierto.");
        if (!world.IsFinite) throw new ArgumentException("World no finito.", nameof(world));
        return new Vector2IntData(
            (int)MathF.Floor(world.X / document.TileSize.X),
            (int)MathF.Floor(world.Y / document.TileSize.Y));
    }

    public Vector2Data CellToWorld(Vector2IntData cell)
    {
        var document = Document ?? throw new InvalidOperationException("No hay mapa abierto.");
        return new Vector2Data(cell.X * document.TileSize.X, cell.Y * document.TileSize.Y);
    }
}
