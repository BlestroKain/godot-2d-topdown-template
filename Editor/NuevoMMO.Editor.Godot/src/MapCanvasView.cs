using Godot;
using NuevoMMO.Core;
using NuevoMMO.Editor;

namespace NuevoMMO.Editor.GodotHost;

public partial class MapCanvasView : Control
{
    private MapEditor? editor;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(640, 480);
    }

    public void Bind(MapEditor mapEditor)
    {
        editor = mapEditor ?? throw new ArgumentNullException(nameof(mapEditor));
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (editor?.Document is not { } document)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(24, 36), "Selecciona o crea un mapa.", HorizontalAlignment.Left, -1, 18);
            return;
        }

        var bounds = document.Bounds;
        var scale = FitScale(bounds);
        var offset = FitOffset(bounds, scale);
        var mapRect = new Rect2(offset, new Vector2(bounds.Width * scale, bounds.Height * scale));
        DrawRect(mapRect, new Color(0.08f, 0.08f, 0.08f), true);
        DrawRect(mapRect, new Color(0.55f, 0.55f, 0.55f), false, 1.5f);

        if (editor.Canvas.GridVisible)
            DrawGrid(document, scale, offset);

        foreach (var region in document.Regions)
            DrawShape(region.Area, scale, offset, new Color(0.25f, 0.85f, 0.45f, 0.16f), new Color(0.25f, 0.85f, 0.45f));

        foreach (var zone in document.SpawnZones)
            DrawShape(zone.Area, scale, offset, new Color(1f, 0.65f, 0.2f, 0.16f), new Color(1f, 0.65f, 0.2f));

        foreach (var portal in document.Portals)
            DrawShape(portal.TriggerArea, scale, offset, new Color(0.2f, 0.55f, 1f, 0.2f), new Color(0.2f, 0.55f, 1f));

        foreach (var collision in document.Collisions)
            DrawShape(collision.Shape, scale, offset, new Color(1f, 0.25f, 0.25f, 0.18f), new Color(1f, 0.25f, 0.25f));

        foreach (var placement in document.Placements)
        {
            var point = WorldToCanvas(placement.Position, bounds, scale, offset);
            var color = placement.Kind switch
            {
                SpawnEntityKind.Mob => new Color(1f, 0.35f, 0.3f),
                SpawnEntityKind.Npc => new Color(0.25f, 0.75f, 1f),
                SpawnEntityKind.Resource => new Color(0.45f, 0.9f, 0.35f),
                _ => Colors.White
            };
            DrawCircle(point, MathF.Max(3f, 5f * scale), color);
        }

        foreach (var evt in editor.Events.List())
        {
            if (evt.Placement is not { } placement) continue;
            var point = WorldToCanvas(placement.Position, bounds, scale, offset);
            DrawCircle(point, MathF.Max(4f, 6f * scale), new Color(0.85f, 0.4f, 1f));
        }

        var spawnPoint = WorldToCanvas(document.Spawn, bounds, scale, offset);
        DrawCircle(spawnPoint, MathF.Max(4f, 6f * scale), new Color(1f, 1f, 0.25f));
    }

    private void DrawGrid(MapDocument document, float scale, Vector2 offset)
    {
        var bounds = document.Bounds;
        var tileWidth = document.TileSize.X;
        var tileHeight = document.TileSize.Y;
        if (tileWidth <= 0 || tileHeight <= 0) return;

        var gridColor = new Color(1f, 1f, 1f, 0.08f);
        for (float x = bounds.Min.X; x <= bounds.Max.X; x += tileWidth)
        {
            var from = WorldToCanvas(new Vector2Data(x, bounds.Min.Y), bounds, scale, offset);
            var to = WorldToCanvas(new Vector2Data(x, bounds.Max.Y), bounds, scale, offset);
            DrawLine(from, to, gridColor, 1f);
        }
        for (float y = bounds.Min.Y; y <= bounds.Max.Y; y += tileHeight)
        {
            var from = WorldToCanvas(new Vector2Data(bounds.Min.X, y), bounds, scale, offset);
            var to = WorldToCanvas(new Vector2Data(bounds.Max.X, y), bounds, scale, offset);
            DrawLine(from, to, gridColor, 1f);
        }
    }

    private void DrawShape(MapShapeDefinition shape, float scale, Vector2 offset, Color fill, Color outline)
    {
        if (editor?.Document is not { } document) return;
        var bounds = document.Bounds;

        switch (shape.Kind)
        {
            case MapShapeKind.Rectangle:
            {
                var center = WorldToCanvas(shape.Center, bounds, scale, offset);
                var size = new Vector2(shape.Size.X * scale, shape.Size.Y * scale);
                var rect = new Rect2(center - size / 2f, size);
                DrawRect(rect, fill, true);
                DrawRect(rect, outline, false, 1.5f);
                break;
            }
            case MapShapeKind.Circle:
            {
                var center = WorldToCanvas(shape.Center, bounds, scale, offset);
                DrawCircle(center, shape.Radius * scale, fill);
                DrawArc(center, shape.Radius * scale, 0, Mathf.Tau, 48, outline, 1.5f);
                break;
            }
            case MapShapeKind.Polygon:
            {
                var points = shape.Points
                    .Select(point => WorldToCanvas(
                        new Vector2Data(shape.Center.X + point.X, shape.Center.Y + point.Y),
                        bounds, scale, offset))
                    .ToArray();
                if (points.Length < 3) return;
                DrawColoredPolygon(points, fill);
                var closed = points.Append(points[0]).ToArray();
                DrawPolyline(closed, outline, 1.5f);
                break;
            }
        }
    }

    private float FitScale(BoundsData bounds)
    {
        var width = MathF.Max(1, Size.X - 32);
        var height = MathF.Max(1, Size.Y - 32);
        return MathF.Max(0.01f, MathF.Min(width / bounds.Width, height / bounds.Height));
    }

    private Vector2 FitOffset(BoundsData bounds, float scale)
    {
        var size = new Vector2(bounds.Width * scale, bounds.Height * scale);
        return (Size - size) / 2f;
    }

    private static Vector2 WorldToCanvas(Vector2Data world, BoundsData bounds, float scale, Vector2 offset)
        => offset + new Vector2((world.X - bounds.Min.X) * scale, (world.Y - bounds.Min.Y) * scale);
}
