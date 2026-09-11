using System.ComponentModel;
using System.Drawing;
using NuevoMMO.Core;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

/// <summary>
/// Documento central del mapeador, equivalente funcional al FrmMapEditor de Intersect.
/// Combina pintura de tiles (fill, arrastre, copy/paste) con placements continuos.
/// </summary>
[DesignerCategory("Form")]
public sealed partial class MapEditorDocument : DockContent
{
    private EditorApplication? application;
    private Vector2Data? pendingCollisionStart;
    private Vector2Data? pendingSpawnZoneStart;
    private Guid? selectedPlacementId;
    private Guid? selectedSpawnZoneId;
    private Vector2Data? pendingShapeStart;
    private Vector2IntData? pendingCellStart;
    private readonly HashSet<Vector2IntData> selectedCells = [];
    private readonly HashSet<Vector2IntData> strokeCells = [];

    public MapEditorDocument()
    {
        InitializeComponent();
        viewport.TabStop = true;
        viewport.WorldClicked += OnWorldClicked;
        viewport.WorldDragged += OnWorldDragged;
        viewport.WorldReleased += OnWorldReleased;
        viewport.KeyDown += OnViewportKeyDown;
    }

    public MapEditorDocument(EditorApplication application, TilesetImageProvider images)
        : this()
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        viewport.Bind(application.Definitions, images);
    }

    public MapEditorTool ActiveTool
    {
        get => viewport.ActiveTool;
        set
        {
            viewport.ActiveTool = value;
            pendingCollisionStart = null;
            pendingSpawnZoneStart = null;
            pendingShapeStart = null;
            pendingCellStart = null;
            strokeCells.Clear();
            viewport.RubberBand = null;
        }
    }

    public DefinitionId? SelectedPlacementDefinitionId { get; set; }
    public GameDefinition? BrushDefinition { get; set; }
    public MapTileClipboard? Clipboard { get; private set; }
    public MapDefinition? CurrentMap => application?.Maps.Document?.ToDefinition();

    public event Action<MapDocument>? MapOpened;
    public event Action? MapChanged;
    public event Action<object?>? SelectedObjectChanged;
    public event Action<string>? EditorNotice;

    public void Open(MapDefinition definition)
    {
        var editor = RequireApplication();
        var document = editor.Maps.Open(definition);
        viewport.Document = document;
        ClearSelection();
        Text = $"Mapa — {document.Name}";
        TabText = document.Name;
        MapOpened?.Invoke(document);
        viewport.Focus();
    }

    public void SaveMap()
    {
        var editor = RequireApplication();
        if (editor.Maps.Document is null) return;
        editor.Maps.Save();
        viewport.RefreshMap();
    }

    public void RefreshView(bool clearAutotiles = true)
    {
        viewport.RefreshMap(clearAutotiles);
        RefreshSelection();
    }

    public void RefreshSelection()
    {
        var editor = application;
        if (editor?.Maps.Document is null)
        {
            ClearSelection();
            return;
        }

        if (selectedPlacementId is { } placementId)
        {
            var placement = editor.Maps.FindPlacement(placementId);
            if (placement is null) ClearSelection();
            else
            {
                viewport.SelectedPlacementId = placement.Id;
                SelectedObjectChanged?.Invoke(placement);
            }
            return;
        }

        if (selectedSpawnZoneId is { } zoneId)
        {
            var zone = editor.Maps.FindSpawnZone(zoneId);
            if (zone is null) ClearSelection();
            else
            {
                viewport.SelectedSpawnZoneId = zone.Id;
                SelectedObjectChanged?.Invoke(zone);
            }
        }
    }

    public bool CopySelection()
    {
        var editor = application;
        if (editor?.Maps.Document is null || selectedCells.Count == 0) return false;
        Clipboard = editor.Maps.Copy(selectedCells);
        return true;
    }

    public bool PasteAtSelection()
    {
        var editor = application;
        if (editor?.Maps.Document is null || Clipboard is null) return false;
        var origin = selectedCells.Count > 0
            ? selectedCells.OrderBy(static cell => cell.Y).ThenBy(static cell => cell.X).First()
            : new Vector2IntData(0, 0);
        if (editor.Maps.Paste(origin, Clipboard) == 0) return false;
        viewport.RefreshMap();
        MapChanged?.Invoke();
        return true;
    }

    private void OnWorldClicked(Vector2Data world, MouseButtons button)
    {
        var editor = application;
        if (button != MouseButtons.Left || editor?.Maps.Document is not { } map) return;

        switch (ActiveTool)
        {
            case MapEditorTool.Select:
                if (viewport.HitTestPlacement(world) is not null
                    || viewport.HitTestSpawnZone(world) is not null
                    || selectedPlacementId is not null
                    || selectedSpawnZoneId is not null)
                    SelectOrMove(world);
                else
                    BeginCellGesture(world, map, clearSelection: true);
                break;
            case MapEditorTool.PaintTile:
            case MapEditorTool.EraseTile:
                BeginStroke(world, map);
                break;
            case MapEditorTool.Fill:
            {
                var cell = viewport.WorldToCell(world);
                if (!IsCellInside(map, cell) || !editor.Maps.Palette.HasSelection) return;
                if (editor.Maps.Fill(cell) == 0) return;
                viewport.RefreshMap();
                MapChanged?.Invoke();
                break;
            }
            case MapEditorTool.Rectangle:
                BeginCellGesture(world, map, clearSelection: true);
                break;
            case MapEditorTool.Collision:
                BeginShape(world);
                break;
            case MapEditorTool.Mob:
                PlaceContent(SpawnEntityKind.Mob, world);
                break;
            case MapEditorTool.Npc:
                PlaceContent(SpawnEntityKind.Npc, world);
                break;
            case MapEditorTool.Resource:
                PlaceContent(SpawnEntityKind.Resource, world);
                break;
            case MapEditorTool.SpawnZone:
                AddSpawnZonePoint(world);
                break;
        }
    }

    private void PlaceContent(SpawnEntityKind kind, Vector2Data world)
    {
        var editor = RequireApplication();
        if (!editor.Maps.Document!.Bounds.Contains(world)) return;
        if (SelectedPlacementDefinitionId is not { } definitionId)
        {
            EditorNotice?.Invoke($"Seleccione una Definition para colocar {kind}.");
            return;
        }

        try
        {
            var placement = editor.Maps.Place(kind, definitionId, world);
            SelectPlacement(placement);
            viewport.RefreshMap(clearAutotileCache: false);
            MapChanged?.Invoke();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            EditorNotice?.Invoke(exception.Message);
        }
    }

    private void SelectOrMove(Vector2Data world)
    {
        var editor = RequireApplication();
        if (editor.Maps.Document is not { } map || !map.Bounds.Contains(world)) return;

        var hitPlacement = viewport.HitTestPlacement(world);
        if (hitPlacement is not null)
        {
            SelectPlacement(hitPlacement);
            return;
        }

        var hitZone = viewport.HitTestSpawnZone(world);
        if (hitZone is not null)
        {
            SelectSpawnZone(hitZone);
            return;
        }

        if (selectedPlacementId is { } placementId)
        {
            try
            {
                var moved = editor.Maps.MovePlacement(placementId, world);
                SelectPlacement(moved);
                viewport.RefreshMap(clearAutotileCache: false);
                MapChanged?.Invoke();
                return;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                EditorNotice?.Invoke(exception.Message);
            }
        }

        ClearSelection();
    }

    private void AddSpawnZonePoint(Vector2Data world)
    {
        var editor = RequireApplication();
        var document = editor.Maps.Document!;
        if (!document.Bounds.Contains(world)) return;
        if (SelectedPlacementDefinitionId is null)
        {
            EditorNotice?.Invoke("Seleccione una SpawnTableDefinition antes de dibujar la zona.");
            return;
        }

        if (pendingSpawnZoneStart is null)
        {
            pendingSpawnZoneStart = world;
            EditorNotice?.Invoke("SpawnZone: marque la esquina opuesta.");
            return;
        }

        var start = pendingSpawnZoneStart.Value;
        pendingSpawnZoneStart = null;
        var width = Math.Abs(world.X - start.X);
        var height = Math.Abs(world.Y - start.Y);
        if (width < 1f || height < 1f)
        {
            EditorNotice?.Invoke("SpawnZone demasiado pequeña.");
            return;
        }

        var center = new Vector2Data((start.X + world.X) / 2f, (start.Y + world.Y) / 2f);
        try
        {
            var area = new MapShapeDefinition(MapShapeKind.Rectangle, center, new(width, height));
            var zone = editor.Maps.AddSpawnZone(SelectedPlacementDefinitionId.Value, area);
            SelectSpawnZone(zone);
            viewport.RefreshMap(clearAutotileCache: false);
            MapChanged?.Invoke();
            EditorNotice?.Invoke("SpawnZone creada.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            EditorNotice?.Invoke(exception.Message);
        }
    }

    private void OnWorldDragged(Vector2Data world, MouseButtons button)
    {
        var editor = application;
        if (button != MouseButtons.Left || editor?.Maps.Document is not { } map) return;

        switch (ActiveTool)
        {
            case MapEditorTool.Select:
            case MapEditorTool.Rectangle:
                if (pendingCellStart is not null)
                    UpdateCellGesture(world, map);
                break;
            case MapEditorTool.PaintTile:
            case MapEditorTool.EraseTile:
                ContinueStroke(world, map);
                break;
            case MapEditorTool.Collision:
                if (pendingShapeStart is { } start)
                    viewport.RubberBand = RectangleFrom(start, world);
                break;
        }
    }

    private void OnWorldReleased(Vector2Data world, MouseButtons button)
    {
        var editor = application;
        if (button != MouseButtons.Left || editor?.Maps.Document is not { } map) return;

        switch (ActiveTool)
        {
            case MapEditorTool.Select:
                if (pendingCellStart is not null)
                    UpdateCellGesture(world, map);
                viewport.RubberBand = null;
                pendingCellStart = null;
                break;
            case MapEditorTool.Rectangle:
                if (pendingCellStart is { } start)
                {
                    var end = viewport.WorldToCell(world);
                    editor.Maps.PaintRect(start, end);
                    viewport.RefreshMap();
                    MapChanged?.Invoke();
                }
                viewport.RubberBand = null;
                pendingCellStart = null;
                break;
            case MapEditorTool.PaintTile:
            case MapEditorTool.EraseTile:
                ApplyStroke();
                break;
            case MapEditorTool.Collision:
                CompleteCollision(world);
                break;
        }
    }

    private void BeginStroke(Vector2Data world, MapDocument map)
    {
        strokeCells.Clear();
        ContinueStroke(world, map);
    }

    private void ContinueStroke(Vector2Data world, MapDocument map)
    {
        var cell = viewport.WorldToCell(world);
        if (!IsCellInside(map, cell)) return;
        strokeCells.Add(cell);
        selectedCells.Clear();
        viewport.SelectedCells = strokeCells;
    }

    private void ApplyStroke()
    {
        var editor = application;
        if (editor?.Maps.Document is null || strokeCells.Count == 0) return;

        var cells = strokeCells.ToArray();
        var changed = ActiveTool == MapEditorTool.EraseTile
            ? editor.Maps.EraseTiles(cells)
            : editor.Maps.PaintTiles(cells);
        strokeCells.Clear();
        viewport.SelectedCells = selectedCells;
        if (changed == 0) return;
        viewport.RefreshMap();
        MapChanged?.Invoke();
    }

    private void BeginCellGesture(Vector2Data world, MapDocument map, bool clearSelection)
    {
        var cell = viewport.WorldToCell(world);
        if (!IsCellInside(map, cell)) return;
        pendingCellStart = cell;
        if (clearSelection) selectedCells.Clear();
        selectedCells.Add(cell);
        viewport.SelectedCells = selectedCells;
        viewport.RubberBand = CellWorldRect(map, cell);
    }

    private void UpdateCellGesture(Vector2Data world, MapDocument map)
    {
        if (pendingCellStart is not { } start) return;
        var end = viewport.WorldToCell(world);
        var cells = RequireApplication().Maps.CellsInRect(start, end);
        selectedCells.Clear();
        foreach (var cell in cells) selectedCells.Add(cell);
        viewport.SelectedCells = selectedCells;
        viewport.RubberBand = RectangleFrom(CellWorldCenter(map, start), world);
    }

    private void BeginShape(Vector2Data world)
    {
        pendingShapeStart = world;
        pendingCollisionStart = world;
        viewport.RubberBand = new RectangleF(world.X, world.Y, 1, 1);
    }

    private void CompleteCollision(Vector2Data world)
    {
        var editor = RequireApplication();
        if (pendingShapeStart is not { } start) return;
        pendingShapeStart = null;
        pendingCollisionStart = null;
        viewport.RubberBand = null;
        var width = Math.Abs(world.X - start.X);
        var height = Math.Abs(world.Y - start.Y);
        if (width < 1 || height < 1) return;
        var center = new Vector2Data((start.X + world.X) / 2f, (start.Y + world.Y) / 2f);
        editor.Maps.Collision.AddRectangle(center, new(width, height));
        editor.Maps.MarkDirty();
        viewport.RefreshMap(clearAutotileCache: false);
        MapChanged?.Invoke();
    }

    private void OnViewportKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            pendingCollisionStart = null;
            pendingSpawnZoneStart = null;
            pendingShapeStart = null;
            pendingCellStart = null;
            viewport.RubberBand = null;
            ClearSelection();
            e.Handled = true;
            return;
        }

        if (e.KeyCode != Keys.Delete) return;
        var editor = application;
        if (editor?.Maps.Document is null) return;

        var changed = selectedPlacementId is { } placementId
            ? editor.Maps.RemovePlacement(placementId)
            : selectedSpawnZoneId is { } zoneId && editor.Maps.RemoveSpawnZone(zoneId);
        if (!changed) return;

        ClearSelection();
        viewport.RefreshMap(clearAutotileCache: false);
        MapChanged?.Invoke();
        e.Handled = true;
    }

    private void SelectPlacement(MapContentPlacementDefinition placement)
    {
        selectedPlacementId = placement.Id;
        selectedSpawnZoneId = null;
        selectedCells.Clear();
        viewport.SelectedCells = selectedCells;
        viewport.SelectedPlacementId = placement.Id;
        SelectedObjectChanged?.Invoke(placement);
    }

    private void SelectSpawnZone(MapSpawnZoneDefinition zone)
    {
        selectedPlacementId = null;
        selectedSpawnZoneId = zone.Id;
        selectedCells.Clear();
        viewport.SelectedCells = selectedCells;
        viewport.SelectedSpawnZoneId = zone.Id;
        SelectedObjectChanged?.Invoke(zone);
    }

    private void ClearSelection()
    {
        selectedPlacementId = null;
        selectedSpawnZoneId = null;
        selectedCells.Clear();
        viewport.SelectedCells = selectedCells;
        viewport.ClearSelection();
        SelectedObjectChanged?.Invoke(application?.Maps.Document);
    }

    private static RectangleF RectangleFrom(Vector2Data start, Vector2Data end)
    {
        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        return new RectangleF(x, y, Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
    }

    private static RectangleF CellWorldRect(MapDocument map, Vector2IntData cell)
        => new(
            map.Bounds.Minimum.X + cell.X * map.TileSize.X,
            map.Bounds.Minimum.Y + cell.Y * map.TileSize.Y,
            map.TileSize.X,
            map.TileSize.Y);

    private static Vector2Data CellWorldCenter(MapDocument map, Vector2IntData cell)
        => new(
            map.Bounds.Minimum.X + (cell.X + 0.5f) * map.TileSize.X,
            map.Bounds.Minimum.Y + (cell.Y + 0.5f) * map.TileSize.Y);

    private static bool IsCellInside(MapDocument map, Vector2IntData cell)
    {
        var width = Math.Max(1, (int)Math.Ceiling(map.Bounds.Width / map.TileSize.X));
        var height = Math.Max(1, (int)Math.Ceiling(map.Bounds.Height / map.TileSize.Y));
        return cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;
    }

    private EditorApplication RequireApplication()
        => application ?? throw new InvalidOperationException("El documento de mapa no está enlazado.");
}
