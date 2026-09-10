using NuevoMMO.Core;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

/// <summary>
/// Documento central del mapeador, equivalente funcional al FrmMapEditor de Intersect.
/// </summary>
public sealed class MapEditorDocument : DockContent
{
    private readonly EditorApplication application;
    private readonly MapViewportControl viewport;
    private Vector2Data? pendingCollisionStart;

    public MapEditorDocument(EditorApplication application, TilesetImageProvider images)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        viewport = new MapViewportControl(application.Definitions, images);
        viewport.WorldClicked += OnWorldClicked;
        Controls.Add(viewport);

        Text = "Mapa";
        TabText = Text;
        CloseButton = false;
        CloseButtonVisible = false;
    }

    public MapEditorTool ActiveTool
    {
        get => viewport.ActiveTool;
        set
        {
            viewport.ActiveTool = value;
            pendingCollisionStart = null;
        }
    }

    public MapDefinition? CurrentMap => application.Maps.Document?.ToDefinition();

    public event Action<MapDocument>? MapOpened;
    public event Action? MapChanged;

    public void Open(MapDefinition definition)
    {
        var document = application.Maps.Open(definition);
        application.Maps.Layers.AddIntersectDefaultsIfEmpty();
        viewport.Document = document;
        Text = $"Mapa — {document.Name}";
        TabText = document.Name;
        MapOpened?.Invoke(document);
        viewport.Focus();
    }

    public void SaveMap()
    {
        if (application.Maps.Document is null) return;
        application.Maps.Save();
        viewport.RefreshMap();
    }

    public void RefreshView(bool clearAutotiles = true) => viewport.RefreshMap(clearAutotiles);

    private void OnWorldClicked(Vector2Data world, MouseButtons button)
    {
        if (button != MouseButtons.Left || application.Maps.Document is not { } map) return;

        switch (ActiveTool)
        {
            case MapEditorTool.PaintTile:
            {
                var cell = viewport.WorldToCell(world);
                if (!IsCellInside(map, cell)) return;
                application.Maps.PaintTile(cell);
                viewport.InvalidateTileNeighborhood(application.Maps.Layers.Active().Key, cell);
                MapChanged?.Invoke();
                break;
            }
            case MapEditorTool.EraseTile:
            {
                var cell = viewport.WorldToCell(world);
                if (!IsCellInside(map, cell)) return;
                if (!application.Maps.EraseTile(cell)) return;
                viewport.InvalidateTileNeighborhood(application.Maps.Layers.Active().Key, cell);
                MapChanged?.Invoke();
                break;
            }
            case MapEditorTool.Collision:
                AddCollisionPoint(world);
                break;
        }
    }

    private void AddCollisionPoint(Vector2Data world)
    {
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
        application.Maps.Collision.AddRectangle(center, new(width, height));
        application.Maps.MarkDirty();
        viewport.RefreshMap(clearAutotiles: false);
        MapChanged?.Invoke();
    }

    private static bool IsCellInside(MapDocument map, Vector2IntData cell)
    {
        var width = Math.Max(1, (int)Math.Ceiling(map.Bounds.Width / map.TileSize.X));
        var height = Math.Max(1, (int)Math.Ceiling(map.Bounds.Height / map.TileSize.Y));
        return cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;
    }
}
