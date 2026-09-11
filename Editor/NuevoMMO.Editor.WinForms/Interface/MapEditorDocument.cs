using System.ComponentModel;
using NuevoMMO.Core;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

/// <summary>
/// Documento central del mapeador, equivalente funcional al FrmMapEditor de Intersect.
/// </summary>
[DesignerCategory("Form")]
public sealed partial class MapEditorDocument : DockContent
{
    private EditorApplication? application;
    private Vector2Data? pendingCollisionStart;
    private Vector2Data? pendingSpawnZoneStart;
    private Guid? selectedPlacementId;
    private Guid? selectedSpawnZoneId;

    public MapEditorDocument()
    {
        InitializeComponent();
        viewport.TabStop = true;
        viewport.WorldClicked += OnWorldClicked;
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
        }
    }

    public DefinitionId? SelectedPlacementDefinitionId { get; set; }
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

    private void OnWorldClicked(Vector2Data world, MouseButtons button)
    {
        var editor = application;
        if (button != MouseButtons.Left || editor?.Maps.Document is not { } map) return;

        switch (ActiveTool)
        {
            case MapEditorTool.Select:
                SelectOrMove(world);
                break;
            case MapEditorTool.PaintTile:
            {
                var cell = viewport.WorldToCell(world);
                if (!IsCellInside(map, cell)) return;
                editor.Maps.PaintTile(cell);
                viewport.InvalidateTileNeighborhood(editor.Maps.Layers.Active().Key, cell);
                MapChanged?.Invoke();
                break;
            }
            case MapEditorTool.EraseTile:
            {
                var cell = viewport.WorldToCell(world);
                if (!IsCellInside(map, cell)) return;
                if (!editor.Maps.EraseTile(cell)) return;
                viewport.InvalidateTileNeighborhood(editor.Maps.Layers.Active().Key, cell);
                MapChanged?.Invoke();
                break;
            }
            case MapEditorTool.Collision:
                AddCollisionPoint(world);
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

    private void AddCollisionPoint(Vector2Data world)
    {
        var editor = RequireApplication();
        if (editor.Maps.Document is not { } document || !document.Bounds.Contains(world)) return;
        if (pendingCollisionStart is null)
        {
            pendingCollisionStart = world;
            return;
        }

        var start = pendingCollisionStart.Value;
        pendingCollisionStart = null;
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
        viewport.SelectedPlacementId = placement.Id;
        SelectedObjectChanged?.Invoke(placement);
    }

    private void SelectSpawnZone(MapSpawnZoneDefinition zone)
    {
        selectedPlacementId = null;
        selectedSpawnZoneId = zone.Id;
        viewport.SelectedSpawnZoneId = zone.Id;
        SelectedObjectChanged?.Invoke(zone);
    }

    private void ClearSelection()
    {
        selectedPlacementId = null;
        selectedSpawnZoneId = null;
        viewport.ClearSelection();
        SelectedObjectChanged?.Invoke(application?.Maps.Document);
    }

    private static bool IsCellInside(MapDocument map, Vector2IntData cell)
    {
        var width = Math.Max(1, (int)Math.Ceiling(map.Bounds.Width / map.TileSize.X));
        var height = Math.Max(1, (int)Math.Ceiling(map.Bounds.Height / map.TileSize.Y));
        return cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;
    }

    private EditorApplication RequireApplication()
        => application ?? throw new InvalidOperationException("El documento de mapa no está enlazado.");
}
