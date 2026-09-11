using NuevoMMO.Core;
using System.Drawing;

namespace NuevoMMO.Editor;

public sealed partial class MapViewportControl
{
    private Guid? selectedPlacementId;
    private Guid? selectedSpawnZoneId;
    private bool selectionPaintHooked;

    public Guid? SelectedPlacementId
    {
        get => selectedPlacementId;
        set
        {
            EnsureSelectionPaintHook();
            if (selectedPlacementId == value && selectedSpawnZoneId is null) return;
            selectedPlacementId = value;
            if (value is not null) selectedSpawnZoneId = null;
            Invalidate();
        }
    }

    public Guid? SelectedSpawnZoneId
    {
        get => selectedSpawnZoneId;
        set
        {
            EnsureSelectionPaintHook();
            if (selectedSpawnZoneId == value && selectedPlacementId is null) return;
            selectedSpawnZoneId = value;
            if (value is not null) selectedPlacementId = null;
            Invalidate();
        }
    }

    public MapContentPlacementDefinition? HitTestPlacement(Vector2Data world, float screenRadius = 14f)
    {
        if (document is null || !ShowPlacements || !world.IsFinite || !float.IsFinite(screenRadius) || screenRadius <= 0)
            return null;

        var radius = screenRadius / zoom;
        var radiusSquared = radius * radius;
        return document.Placements
            .Select(value => new { Placement = value, Distance = DistanceSquared(value.Position, world) })
            .Where(value => value.Distance <= radiusSquared)
            .OrderBy(value => value.Distance)
            .Select(value => value.Placement)
            .FirstOrDefault();
    }

    public MapSpawnZoneDefinition? HitTestSpawnZone(Vector2Data world)
    {
        if (document is null || !ShowSpawnZones || !world.IsFinite) return null;
        return document.SpawnZones.LastOrDefault(zone => Contains(zone.Area, world));
    }

    public void ClearSelection()
    {
        selectedPlacementId = null;
        selectedSpawnZoneId = null;
        Invalidate();
    }

    private void EnsureSelectionPaintHook()
    {
        if (selectionPaintHooked) return;
        selectionPaintHooked = true;
        Paint += (_, args) => DrawSelection(args.Graphics);
    }

    private void DrawSelection(Graphics graphics)
    {
        if (document is null) return;

        if (selectedPlacementId is { } placementId)
        {
            var placement = document.Placements.FirstOrDefault(value => value.Id == placementId);
            if (placement is not null)
            {
                var screen = WorldToScreen(placement.Position);
                const float radius = 15f;
                using var pen = new Pen(Color.White, 2f);
                graphics.DrawEllipse(pen, screen.X - radius, screen.Y - radius, radius * 2, radius * 2);
            }
        }

        if (selectedSpawnZoneId is { } zoneId)
        {
            var zone = document.SpawnZones.FirstOrDefault(value => value.Id == zoneId);
            if (zone is not null)
            {
                var state = graphics.Save();
                ApplyWorldTransform(graphics);
                using var pen = new Pen(Color.White, 3f / zoom);
                DrawShape(graphics, zone.Area, pen);
                graphics.Restore(state);
            }
        }
    }

    private static float DistanceSquared(Vector2Data left, Vector2Data right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return dx * dx + dy * dy;
    }

    private static bool Contains(MapShapeDefinition shape, Vector2Data point)
        => shape.Kind switch
        {
            MapShapeKind.Rectangle =>
                Math.Abs(point.X - shape.Center.X) <= shape.Size.X / 2f &&
                Math.Abs(point.Y - shape.Center.Y) <= shape.Size.Y / 2f,
            MapShapeKind.Circle => DistanceSquared(shape.Center, point) <= shape.Radius * shape.Radius,
            MapShapeKind.Polygon => ContainsPolygon(shape, point),
            _ => false
        };

    private static bool ContainsPolygon(MapShapeDefinition shape, Vector2Data point)
    {
        var inside = false;
        var points = shape.Points;
        for (var i = 0; i < points.Length; i++)
        {
            var j = (i + points.Length - 1) % points.Length;
            var xi = shape.Center.X + points[i].X;
            var yi = shape.Center.Y + points[i].Y;
            var xj = shape.Center.X + points[j].X;
            var yj = shape.Center.Y + points[j].Y;
            var intersects = (yi > point.Y) != (yj > point.Y) &&
                             point.X < (xj - xi) * (point.Y - yi) / (yj - yi) + xi;
            if (intersects) inside = !inside;
        }
        return inside;
    }
}
