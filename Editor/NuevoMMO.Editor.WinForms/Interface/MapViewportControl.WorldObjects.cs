using NuevoMMO.Core;
using System.Drawing;

namespace NuevoMMO.Editor;

public sealed partial class MapViewportControl
{
    private bool worldObjectPaintHooked;
    private Guid? selectedPortalId;
    private Guid? selectedRegionId;
    private Guid? selectedLightId;
    private DefinitionId? selectedEventId;

    public Guid? SelectedPortalId { get => selectedPortalId; set { selectedPortalId = value; Invalidate(); } }
    public Guid? SelectedRegionId { get => selectedRegionId; set { selectedRegionId = value; Invalidate(); } }
    public Guid? SelectedLightId { get => selectedLightId; set { selectedLightId = value; Invalidate(); } }
    public DefinitionId? SelectedEventId { get => selectedEventId; set { selectedEventId = value; Invalidate(); } }

    public void EnableWorldObjectOverlays()
    {
        if (worldObjectPaintHooked) return;
        worldObjectPaintHooked = true;
        Paint += (_, args) => DrawWorldObjectOverlay(args.Graphics);
    }

    public void ClearWorldObjectSelection()
    {
        selectedPortalId = null;
        selectedRegionId = null;
        selectedLightId = null;
        selectedEventId = null;
        Invalidate();
    }

    public MapPortalDefinition? HitTestPortal(Vector2Data world)
        => document is null || !ShowPortals ? null : document.Portals.LastOrDefault(value => ContainsWorldShape(value.TriggerArea, world));

    public MapRegionDefinition? HitTestRegion(Vector2Data world)
        => document is null || !ShowRegions ? null : document.Regions.LastOrDefault(value => ContainsWorldShape(value.Area, world));

    public MapLightDefinition? HitTestLight(Vector2Data world)
    {
        if (document is null) return null;
        var hitRadius = 14f / zoom;
        return document.Lights
            .Select(value => new { Light = value, Distance = WorldDistanceSquared(value.Position, world) })
            .Where(value => value.Distance <= Math.Max(hitRadius * hitRadius, value.Light.Radius * value.Light.Radius))
            .OrderBy(value => value.Distance)
            .Select(value => value.Light)
            .FirstOrDefault();
    }

    public EventDefinition? HitTestMapEvent(Vector2Data world, float screenRadius = 14f)
    {
        if (document is null || !ShowEvents || registry is null) return null;
        var radius = screenRadius / zoom;
        var radiusSquared = radius * radius;
        return Registry.GetAll<EventDefinition>()
            .Where(value => value.Scope == EventScope.Map && value.Placement?.MapId == document.Id)
            .Select(value => new { Event = value, Distance = WorldDistanceSquared(value.Placement!.Position, world) })
            .Where(value => value.Distance <= radiusSquared)
            .OrderBy(value => value.Distance)
            .Select(value => value.Event)
            .FirstOrDefault();
    }

    private void DrawWorldObjectOverlay(Graphics graphics)
    {
        if (document is null) return;
        var state = graphics.Save();
        ApplyWorldTransform(graphics);

        using (var lightPen = new Pen(Color.FromArgb(170, 255, 225, 90), 1.5f / zoom))
        {
            foreach (var light in document.Lights)
            {
                graphics.DrawEllipse(
                    lightPen,
                    light.Position.X - light.Radius,
                    light.Position.Y - light.Radius,
                    light.Radius * 2,
                    light.Radius * 2);
                DrawMarker(graphics, light.Position, "L", Color.Gold);
            }
        }

        using var selectedPen = new Pen(Color.White, 3f / zoom);
        if (selectedPortalId is { } portalId)
        {
            var portal = document.Portals.FirstOrDefault(value => value.Id == portalId);
            if (portal is not null) DrawShape(graphics, portal.TriggerArea, selectedPen);
        }
        if (selectedRegionId is { } regionId)
        {
            var region = document.Regions.FirstOrDefault(value => value.Id == regionId);
            if (region is not null) DrawShape(graphics, region.Area, selectedPen);
        }
        if (selectedLightId is { } lightId)
        {
            var light = document.Lights.FirstOrDefault(value => value.Id == lightId);
            if (light is not null)
                graphics.DrawEllipse(selectedPen, light.Position.X - light.Radius, light.Position.Y - light.Radius, light.Radius * 2, light.Radius * 2);
        }
        if (selectedEventId is { } eventId && registry is not null && Registry.TryGet<EventDefinition>(eventId, out var evt) && evt?.Placement is { } placement)
        {
            var radius = 14f / zoom;
            graphics.DrawEllipse(selectedPen, placement.Position.X - radius, placement.Position.Y - radius, radius * 2, radius * 2);
        }

        graphics.Restore(state);
    }

    private static float WorldDistanceSquared(Vector2Data left, Vector2Data right)
    {
        var x = left.X - right.X;
        var y = left.Y - right.Y;
        return x * x + y * y;
    }

    private static bool ContainsWorldShape(MapShapeDefinition shape, Vector2Data point)
        => shape.Kind switch
        {
            MapShapeKind.Rectangle =>
                Math.Abs(point.X - shape.Center.X) <= shape.Size.X / 2f &&
                Math.Abs(point.Y - shape.Center.Y) <= shape.Size.Y / 2f,
            MapShapeKind.Circle => WorldDistanceSquared(shape.Center, point) <= shape.Radius * shape.Radius,
            MapShapeKind.Polygon => ContainsWorldPolygon(shape, point),
            _ => false
        };

    private static bool ContainsWorldPolygon(MapShapeDefinition shape, Vector2Data point)
    {
        var inside = false;
        for (var index = 0; index < shape.Points.Length; index++)
        {
            var previous = (index + shape.Points.Length - 1) % shape.Points.Length;
            var ax = shape.Center.X + shape.Points[index].X;
            var ay = shape.Center.Y + shape.Points[index].Y;
            var bx = shape.Center.X + shape.Points[previous].X;
            var by = shape.Center.Y + shape.Points[previous].Y;
            var intersects = (ay > point.Y) != (by > point.Y) &&
                             point.X < (bx - ax) * (point.Y - ay) / (by - ay) + ax;
            if (intersects) inside = !inside;
        }
        return inside;
    }
}
