using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed partial class MainForm
{
    private bool tilesetPaintingEventsWired;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (tilesetPaintingEventsWired) return;

        tilesetPaintingEventsWired = true;
        tilesetPalette.TileBrushSelected += OnTileBrushSelected;
    }

    private void OnTileBrushSelected(TilesetDefinition definition, Vector2IntData cell)
    {
        // Flujo clásico de Intersect: seleccionar un tile convierte ese tile en el pincel activo
        // y deja el lienzo listo para pintar inmediatamente con clic o arrastre.
        SetMapTool(MapEditorTool.PaintTile);
        mapDocument.Activate();
        mapDocument.FocusCanvas();
        SetStatus($"Pintar · {definition.Name} · tile {cell.X},{cell.Y}");
    }
}
