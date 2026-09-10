using NuevoMMO.Core;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

/// <summary>
/// Lienzo WinForms del editor de mapas. Renderiza el mismo modelo de tiles/autotiles que consumirá
/// el cliente, manteniendo colisiones, regiones, spawns y eventos como overlays independientes.
/// </summary>
public sealed class MapViewportControl : Control
{
    private readonly DefinitionRegistry registry;
    private readonly TilesetImageProvider images;
    private readonly System.Windows.Forms.Timer animationTimer;
    private readonly Dictionary<string, Dictionary<Vector2IntData, MapAutotileRenderData>> autotileCache =
        new(StringComparer.OrdinalIgnoreCase);

    private MapDocument? document;
    private float zoom = 1f;
    private PointF pan = new(24, 24);
    private bool panning;
    private Point lastMouse;

    public MapViewportControl(DefinitionRegistry registry, TilesetImageProvider images)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.images = images ?? throw new ArgumentNullException(nameof(images));

        DoubleBuffered = true;
        BackColor = Color.FromArgb(28, 30, 34);
        Dock = DockStyle.Fill;
        TabStop = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

        animationTimer = new System.Windows.Forms.Timer { Interval = 50 };
        animationTimer.Tick += (_, _) =>
        {
            if (document is not null && HasAnimatedTiles()) Invalidate();
        };
        animationTimer.Start();

        MouseWheel += OnMouseWheelZoom;
        MouseDown += OnViewportMouseDown;
        MouseMove += OnViewportMouseMove;
        MouseUp += OnViewportMouseUp;
    }

    public MapDocument? Document
    {
        get => document;
        set
        {
            document = value;
            ClearAutotileCache();
            Invalidate();
        }
    }

    public MapEditorTool ActiveTool { get; set; } = MapEditorTool.Select;
    public bool ShowGrid { get; set; } = true;
    public bool ShowCollisions { get; set; } = true;
    public bool ShowRegions { get; set; } = true;
    public bool ShowSpawnZones { get; set; } = true;
    public bool ShowPortals { get; set; } = true;
    public bool ShowPlacements { get; set; } = true;
    public bool ShowEvents { get; set; } = true;

    public float Zoom
    {
        get => zoom;
        set
        {
            zoom = Math.Clamp(value, 0.25f, 8f);
            Invalidate();
        }
    }

    public event Action<Vector2Data, MouseButtons>? WorldClicked;

    public void RefreshMap(bool clearAutotileCache = true)
    {
        if (clearAutotileCache) ClearAutotileCache();
        Invalidate();
    }

    public void InvalidateTileNeighborhood(string layerKey, Vector2IntData changedCell)
    {
        if (document is null) return;
        var layer = document.Layers.FirstOrDefault(value =>
            string.Equals(value.Key, layerKey, StringComparison.OrdinalIgnoreCase));
        if (layer is null) return;

        // El cache por capa es barato de regenerar y evita estados parciales incorrectos con cliffs.
        autotileCache.Remove(layer.Key);
        Invalidate(WorldCellScreenRectangle(changedCell));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) animationTimer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (document is null) return;

        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.SmoothingMode = SmoothingMode.None;

        var state = e.Graphics.Save();
        ApplyWorldTransform(e.Graphics);

        DrawLayers(e.Graphics, MapLayerBand.Lower);
        DrawWorldObjects(e.Graphics);
        DrawLayers(e.Graphics, MapLayerBand.Middle);
        DrawLayers(e.Graphics, MapLayerBand.Upper);
        DrawEditorOverlays(e.Graphics);
        if (ShowGrid) DrawGrid(e.Graphics);
        DrawSpawnPoint(e.Graphics);
        DrawMapBounds(e.Graphics);

        e.Graphics.Restore(state);
    }

    private void DrawLayers(Graphics graphics, MapLayerBand band)
    {
        var map = RequireDocument();
        foreach (var layer in map.Layers
                     .Where(value => value.Visible && value.Band == band)
                     .OrderBy(value => value.Order))
        {
            var resolved = ResolveLayer(layer);
            foreach (var tile in layer.Tiles)
                DrawTile(graphics, map, tile, resolved.GetValueOrDefault(tile.Cell));
        }
    }

    private void DrawTile(
        Graphics graphics,
        MapDocument map,
        MapTilePlacementDefinition tile,
        MapAutotileRenderData? resolved)
    {
        if (!registry.TryGet<TilesetDefinition>(tile.TilesetKey, out var tileset) || tileset is null)
        {
            DrawMissingTile(graphics, map, tile.Cell);
            return;
        }

        if (!images.TryGet(tile.TilesetKey, out var image) || image is null)
        {
            DrawMissingTile(graphics, map, tile.Cell);
            return;
        }

        var tileSize = map.TileSize;
        var worldX = map.Bounds.Min.X + tile.Cell.X * tileSize.X;
        var worldY = map.Bounds.Min.Y + tile.Cell.Y * tileSize.Y;
        var destination = new RectangleF(worldX, worldY, tileSize.X, tileSize.Y);
        var frame = CurrentFrame(tile.Autotile, tileset);
        var frameOffset = MapAutotileRenderData.FrameOffset(
            tile.Autotile,
            frame.autotileFrame,
            frame.waterfallFrame,
            tileSize);

        var transformState = graphics.Save();
        ApplyTileTransform(graphics, destination, tile);

        if (resolved?.RenderState == MapTileRenderState.Autotile)
        {
            var halfWidth = tileSize.X / 2;
            var halfHeight = tileSize.Y / 2;
            for (var quarter = 0; quarter < 4; quarter++)
            {
                var sourcePoint = resolved.QuarterSourcePixels[quarter] + frameOffset;
                var source = new Rectangle(sourcePoint.X, sourcePoint.Y, halfWidth, halfHeight);
                if (!InsideImage(image, source)) continue;

                var dx = quarter is 1 or 3 ? halfWidth : 0;
                var dy = quarter >= 2 ? halfHeight : 0;
                var target = new RectangleF(worldX + dx, worldY + dy, halfWidth, halfHeight);
                graphics.DrawImage(image, target, source, GraphicsUnit.Pixel);
            }
        }
        else
        {
            var sourceX = tile.AtlasCell.X * tileSize.X;
            var sourceY = tile.AtlasCell.Y * tileSize.Y;
            var source = new Rectangle(sourceX, sourceY, tileSize.X, tileSize.Y);
            if (InsideImage(image, source))
                graphics.DrawImage(image, destination, source, GraphicsUnit.Pixel);
        }

        graphics.Restore(transformState);
    }

    private Dictionary<Vector2IntData, MapAutotileRenderData> ResolveLayer(MapLayerDefinition layer)
    {
        if (autotileCache.TryGetValue(layer.Key, out var cache)) return cache;

        var map = RequireDocument();
        var width = Math.Max(1, (int)Math.Ceiling(map.Bounds.Width / map.TileSize.X));
        var height = Math.Max(1, (int)Math.Ceiling(map.Bounds.Height / map.TileSize.Y));
        var resolver = new MapAutotileResolver(layer, width, height, map.TileSize);
        cache = resolver.ResolveAll();
        autotileCache[layer.Key] = cache;
        return cache;
    }

    private void DrawWorldObjects(Graphics graphics)
    {
        if (!ShowPlacements) return;
        var map = RequireDocument();
        foreach (var placement in map.Placements)
        {
            var label = placement.Kind switch
            {
                SpawnEntityKind.Mob => "M",
                SpawnEntityKind.Npc => "N",
                SpawnEntityKind.Resource => "R",
                _ => "?"
            };
            DrawMarker(graphics, placement.Position, label, Color.Goldenrod);
        }
    }

    private void DrawEditorOverlays(Graphics graphics)
    {
        var map = RequireDocument();

        if (ShowRegions)
        {
            using var pen = new Pen(Color.FromArgb(190, 80, 170, 255), 2f / zoom);
            foreach (var region in map.Regions) DrawShape(graphics, region.Area, pen);
        }

        if (ShowSpawnZones)
        {
            using var pen = new Pen(Color.FromArgb(190, 80, 220, 120), 2f / zoom) { DashStyle = DashStyle.Dash };
            foreach (var zone in map.SpawnZones) DrawShape(graphics, zone.Area, pen);
        }

        if (ShowPortals)
        {
            using var pen = new Pen(Color.FromArgb(220, 200, 100, 255), 2f / zoom);
            foreach (var portal in map.Portals) DrawShape(graphics, portal.TriggerArea, pen);
        }

        if (ShowCollisions)
        {
            using var movementPen = new Pen(Color.FromArgb(220, 240, 70, 70), 2f / zoom);
            using var otherPen = new Pen(Color.FromArgb(180, 255, 160, 70), 1f / zoom);
            foreach (var collision in map.Collisions)
                DrawShape(graphics, collision.Shape, collision.BlocksMovement ? movementPen : otherPen);
        }

        if (ShowEvents)
        {
            foreach (var evt in registry.GetAll<EventDefinition>())
            {
                if (evt.Placement is not { } placement || placement.MapId != map.Id) continue;
                DrawMarker(graphics, placement.Position, "E", Color.DeepSkyBlue);
            }
        }
    }

    private void DrawGrid(Graphics graphics)
    {
        var map = RequireDocument();
        var size = map.TileSize;
        using var pen = new Pen(Color.FromArgb(45, 220, 220, 220), 1f / zoom);

        for (var x = map.Bounds.Min.X; x <= map.Bounds.Max.X; x += size.X)
            graphics.DrawLine(pen, x, map.Bounds.Min.Y, x, map.Bounds.Max.Y);
        for (var y = map.Bounds.Min.Y; y <= map.Bounds.Max.Y; y += size.Y)
            graphics.DrawLine(pen, map.Bounds.Min.X, y, map.Bounds.Max.X, y);
    }

    private void DrawSpawnPoint(Graphics graphics)
    {
        var map = RequireDocument();
        using var pen = new Pen(Color.LimeGreen, 2f / zoom);
        var radius = 7f / zoom;
        graphics.DrawEllipse(pen, map.Spawn.X - radius, map.Spawn.Y - radius, radius * 2, radius * 2);
        graphics.DrawLine(pen, map.Spawn.X - radius * 1.5f, map.Spawn.Y, map.Spawn.X + radius * 1.5f, map.Spawn.Y);
        graphics.DrawLine(pen, map.Spawn.X, map.Spawn.Y - radius * 1.5f, map.Spawn.X, map.Spawn.Y + radius * 1.5f);
    }

    private void DrawMapBounds(Graphics graphics)
    {
        var map = RequireDocument();
        using var pen = new Pen(Color.White, 2f / zoom);
        graphics.DrawRectangle(
            pen,
            map.Bounds.Min.X,
            map.Bounds.Min.Y,
            map.Bounds.Width,
            map.Bounds.Height);
    }

    private static void DrawShape(Graphics graphics, MapShapeDefinition shape, Pen pen)
    {
        switch (shape.Kind)
        {
            case MapShapeKind.Rectangle:
                graphics.DrawRectangle(
                    pen,
                    shape.Center.X - shape.Size.X / 2,
                    shape.Center.Y - shape.Size.Y / 2,
                    shape.Size.X,
                    shape.Size.Y);
                break;
            case MapShapeKind.Circle:
                graphics.DrawEllipse(
                    pen,
                    shape.Center.X - shape.Radius,
                    shape.Center.Y - shape.Radius,
                    shape.Radius * 2,
                    shape.Radius * 2);
                break;
            case MapShapeKind.Polygon:
                if (shape.Points.Length >= 3)
                {
                    var points = shape.Points
                        .Select(point => new PointF(shape.Center.X + point.X, shape.Center.Y + point.Y))
                        .ToArray();
                    graphics.DrawPolygon(pen, points);
                }
                break;
        }
    }

    private void DrawMarker(Graphics graphics, Vector2Data position, string text, Color color)
    {
        var radius = 9f / zoom;
        using var brush = new SolidBrush(Color.FromArgb(210, color));
        using var pen = new Pen(Color.Black, 1f / zoom);
        graphics.FillEllipse(brush, position.X - radius, position.Y - radius, radius * 2, radius * 2);
        graphics.DrawEllipse(pen, position.X - radius, position.Y - radius, radius * 2, radius * 2);

        var state = graphics.Save();
        graphics.ResetTransform();
        var screen = WorldToScreen(position);
        TextRenderer.DrawText(
            graphics,
            text,
            Font,
            new Point((int)screen.X - 5, (int)screen.Y - 8),
            Color.Black,
            TextFormatFlags.NoPadding);
        graphics.Restore(state);
    }

    private void DrawMissingTile(Graphics graphics, MapDocument map, Vector2IntData cell)
    {
        var x = map.Bounds.Min.X + cell.X * map.TileSize.X;
        var y = map.Bounds.Min.Y + cell.Y * map.TileSize.Y;
        using var pen = new Pen(Color.Magenta, 2f / zoom);
        graphics.DrawRectangle(pen, x, y, map.TileSize.X, map.TileSize.Y);
        graphics.DrawLine(pen, x, y, x + map.TileSize.X, y + map.TileSize.Y);
        graphics.DrawLine(pen, x + map.TileSize.X, y, x, y + map.TileSize.Y);
    }

    private static void ApplyTileTransform(Graphics graphics, RectangleF rect, MapTilePlacementDefinition tile)
    {
        if (tile.RotationQuarterTurns == 0 && !tile.FlipHorizontal && !tile.FlipVertical) return;

        var centerX = rect.Left + rect.Width / 2;
        var centerY = rect.Top + rect.Height / 2;
        graphics.TranslateTransform(centerX, centerY, MatrixOrder.Append);
        if (tile.RotationQuarterTurns != 0)
            graphics.RotateTransform(tile.RotationQuarterTurns * 90f, MatrixOrder.Append);
        graphics.ScaleTransform(tile.FlipHorizontal ? -1 : 1, tile.FlipVertical ? -1 : 1, MatrixOrder.Append);
        graphics.TranslateTransform(-centerX, -centerY, MatrixOrder.Append);
    }

    private (int autotileFrame, int waterfallFrame) CurrentFrame(MapAutotileMode mode, TilesetDefinition tileset)
    {
        var now = Environment.TickCount64;
        var autotileFrame = mode is MapAutotileMode.Animated or MapAutotileMode.AnimatedXp
            ? (int)((now / tileset.AutotileFrameMilliseconds) % tileset.AutotileAnimationFrames)
            : 0;
        var waterfallFrame = mode == MapAutotileMode.Waterfall
            ? (int)((now / tileset.WaterfallFrameMilliseconds) % tileset.WaterfallAnimationFrames)
            : 1;
        return (autotileFrame, waterfallFrame);
    }

    private bool HasAnimatedTiles()
        => document?.Layers.Any(layer => layer.Tiles.Any(tile =>
            tile.Autotile is MapAutotileMode.Animated or MapAutotileMode.AnimatedXp or MapAutotileMode.Waterfall)) == true;

    private void ApplyWorldTransform(Graphics graphics)
    {
        graphics.TranslateTransform(pan.X, pan.Y);
        graphics.ScaleTransform(zoom, zoom);
    }

    private void OnMouseWheelZoom(object? sender, MouseEventArgs e)
    {
        var before = ScreenToWorld(e.Location);
        var factor = e.Delta > 0 ? 1.15f : 1f / 1.15f;
        zoom = Math.Clamp(zoom * factor, 0.25f, 8f);
        var after = WorldToScreen(before);
        pan.X += e.X - after.X;
        pan.Y += e.Y - after.Y;
        Invalidate();
    }

    private void OnViewportMouseDown(object? sender, MouseEventArgs e)
    {
        Focus();
        if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Right && ActiveTool == MapEditorTool.Select))
        {
            panning = true;
            lastMouse = e.Location;
            Cursor = Cursors.Hand;
            return;
        }

        WorldClicked?.Invoke(ScreenToWorld(e.Location), e.Button);
    }

    private void OnViewportMouseMove(object? sender, MouseEventArgs e)
    {
        if (!panning) return;
        pan.X += e.X - lastMouse.X;
        pan.Y += e.Y - lastMouse.Y;
        lastMouse = e.Location;
        Invalidate();
    }

    private void OnViewportMouseUp(object? sender, MouseEventArgs e)
    {
        if (!panning) return;
        panning = false;
        Cursor = Cursors.Default;
    }

    public Vector2Data ScreenToWorld(Point screen)
        => new((screen.X - pan.X) / zoom, (screen.Y - pan.Y) / zoom);

    public PointF WorldToScreen(Vector2Data world)
        => new(world.X * zoom + pan.X, world.Y * zoom + pan.Y);

    public Vector2IntData WorldToCell(Vector2Data world)
    {
        var map = RequireDocument();
        return new Vector2IntData(
            (int)Math.Floor((world.X - map.Bounds.Min.X) / map.TileSize.X),
            (int)Math.Floor((world.Y - map.Bounds.Min.Y) / map.TileSize.Y));
    }

    private Rectangle WorldCellScreenRectangle(Vector2IntData cell)
    {
        var map = RequireDocument();
        var world = new Vector2Data(
            map.Bounds.Min.X + cell.X * map.TileSize.X,
            map.Bounds.Min.Y + cell.Y * map.TileSize.Y);
        var topLeft = WorldToScreen(world);
        return Rectangle.Ceiling(new RectangleF(
            topLeft.X - 2,
            topLeft.Y - 2,
            map.TileSize.X * zoom + 4,
            map.TileSize.Y * zoom + 4));
    }

    private static bool InsideImage(Image image, Rectangle source)
        => source.X >= 0 && source.Y >= 0 && source.Right <= image.Width && source.Bottom <= image.Height;

    private void ClearAutotileCache() => autotileCache.Clear();

    private MapDocument RequireDocument()
        => document ?? throw new InvalidOperationException("No hay mapa abierto.");
}
