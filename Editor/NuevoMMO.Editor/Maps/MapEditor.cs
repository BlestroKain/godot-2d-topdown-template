using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Coordinador del editor de mapas. Todas las herramientas operan sobre el mismo MapDocument.
/// La UI Godot puede cambiar de herramienta sin crear copias divergentes del mapa.
/// </summary>
public sealed class MapEditor
{
    private readonly MapDefinitionEditor definitions;
    private readonly EditorHistory history;
    private readonly DirtyState dirty;

    public MapEditor()
        : this(new DefinitionRegistry(), new EditorHistory(), new DirtyState())
    {
    }

    public MapEditor(DefinitionRegistry registry, EditorHistory history, DirtyState dirty)
    {
        definitions = new MapDefinitionEditor(registry ?? throw new ArgumentNullException(nameof(registry)));
        this.history = history ?? throw new ArgumentNullException(nameof(history));
        this.dirty = dirty ?? throw new ArgumentNullException(nameof(dirty));
        Events = new MapEventPlacementEditor(registry);
    }

    public MapDocument? Document { get; private set; }
    public MapCanvas Canvas { get; } = new();
    public TilePalette Palette { get; } = new();
    public LayerManager Layers { get; } = new();
    public CollisionEditor Collision { get; } = new();
    public SpawnEditor Spawns { get; } = new();
    public ResourceNodeEditor Resources { get; } = new();
    public PortalEditor Portals { get; } = new();
    public RegionEditor Regions { get; } = new();
    public EnvironmentEditor Environment { get; } = new();
    public MapEventPlacementEditor Events { get; }

    public MapDocument Open(DefinitionId id) => Open(definitions.Edit(id));

    public MapDocument Open(MapDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var document = MapDocument.FromDefinition(definition);
        Bind(document);
        return document;
    }

    public MapDocument Create(MapDefinition definition)
    {
        definitions.Create(definition);
        dirty.Mark();
        return Open(definition);
    }

    public MapDefinition Save()
    {
        var map = RequireDocument().ToDefinition();
        definitions.Upsert(map);
        dirty.Clear();
        return map;
    }

    public void Close()
    {
        Document = null;
        Canvas.Clear();
    }

    public void PaintTile(Vector2IntData cell)
    {
        var previous = Layers.Active();
        var placement = Palette.CreatePlacement(cell);
        var tiles = previous.Tiles.Where(tile => tile.Cell != cell).Append(placement).ToArray();
        var replacement = CopyLayer(previous, tiles);

        history.Push(new ChangeSet(
            $"Pintar tile {cell}",
            () => { Layers.Replace(replacement); dirty.Mark(); },
            () => { Layers.Replace(previous); dirty.Mark(); }));
    }

    public bool EraseTile(Vector2IntData cell)
    {
        var previous = Layers.Active();
        if (!previous.Tiles.Any(tile => tile.Cell == cell)) return false;
        var replacement = CopyLayer(previous, previous.Tiles.Where(tile => tile.Cell != cell).ToArray());

        history.Push(new ChangeSet(
            $"Borrar tile {cell}",
            () => { Layers.Replace(replacement); dirty.Mark(); },
            () => { Layers.Replace(previous); dirty.Mark(); }));
        return true;
    }

    public MapDefinition PreviewDefinition() => RequireDocument().ToDefinition();

    public void MarkDirty() => dirty.Mark();

    private void Bind(MapDocument document)
    {
        Document = document;
        Canvas.Bind(document);
        Layers.Bind(document);
        Collision.Bind(document);
        Spawns.Bind(document);
        Resources.Bind(document);
        Portals.Bind(document);
        Regions.Bind(document);
        Environment.Bind(document);
        Events.Bind(document.Id);
    }

    private MapDocument RequireDocument()
        => Document ?? throw new InvalidOperationException("No hay mapa abierto.");

    private static MapLayerDefinition CopyLayer(MapLayerDefinition source, MapTilePlacementDefinition[] tiles)
        => new(
            source.Key,
            source.Order,
            tiles,
            source.Visible,
            source.ParallaxFactor,
            source.Parameters);
}
