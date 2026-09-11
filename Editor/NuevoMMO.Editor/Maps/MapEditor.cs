using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Coordinador del editor de mapas. Todas las herramientas operan sobre el mismo MapDocument.
/// La UI WinForms cambia de herramienta sin crear copias divergentes del mapa.
/// </summary>
public sealed class MapEditor
{
    private readonly DefinitionRegistry registry;
    private readonly MapDefinitionEditor definitions;
    private readonly EditorHistory history;
    private readonly DirtyState dirty;

    public MapEditor()
        : this(new DefinitionRegistry(), new EditorHistory(), new DirtyState())
    {
    }

    public MapEditor(DefinitionRegistry registry, EditorHistory history, DirtyState dirty)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        definitions = new MapDefinitionEditor(this.registry);
        this.history = history ?? throw new ArgumentNullException(nameof(history));
        this.dirty = dirty ?? throw new ArgumentNullException(nameof(dirty));
        Events = new MapEventPlacementEditor(this.registry);
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
        var document = Open(definition);
        EnsureDefaultLayers();
        return document;
    }

    public MapDefinition Save()
    {
        var map = RequireDocument().ToDefinition();
        definitions.Upsert(map);
        return map;
    }

    /// <summary>
    /// Inserta las capas Intersect por defecto solo cuando el documento no tiene ninguna.
    /// Marca Dirty porque muta el mapa; no se llama al abrir contenido existente.
    /// </summary>
    public bool EnsureDefaultLayers()
    {
        if (Document is null) throw new InvalidOperationException("No hay mapa abierto.");
        if (Document.Layers.Count > 0) return false;
        Layers.AddIntersectDefaultsIfEmpty();
        dirty.Mark();
        return true;
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

    /// <summary>
    /// Devuelve únicamente Definitions compatibles con el tipo de placement.
    /// La selección visual del editor nunca mezcla mobs, NPCs y recursos.
    /// </summary>
    public IReadOnlyList<GameDefinition> PlacementDefinitions(SpawnEntityKind kind)
        => kind switch
        {
            SpawnEntityKind.Mob => registry.GetAll<MobDefinition>().Cast<GameDefinition>().OrderBy(static value => value.Name).ToArray(),
            SpawnEntityKind.Npc => registry.GetAll<NpcDefinition>().Cast<GameDefinition>().OrderBy(static value => value.Name).ToArray(),
            SpawnEntityKind.Resource => registry.GetAll<ResourceDefinition>().Cast<GameDefinition>().OrderBy(static value => value.Name).ToArray(),
            _ => []
        };

    public IReadOnlyList<SpawnTableDefinition> SpawnTableDefinitions()
        => registry.GetAll<SpawnTableDefinition>().OrderBy(static value => value.Name).ToArray();

    /// <summary>
    /// Coloca una entidad de contenido en coordenadas continuas de mundo. No se ajusta a la grilla:
    /// los tiles son una ayuda visual, no la unidad física de movimiento/placement.
    /// </summary>
    public MapContentPlacementDefinition Place(
        SpawnEntityKind kind,
        DefinitionId definitionId,
        Vector2Data position,
        Direction direction = Direction.Down)
    {
        var document = RequireDocument();
        ValidatePlacementDefinition(kind, definitionId);
        EnsureInsideMap(document, position);

        var placement = new MapContentPlacementDefinition(Guid.NewGuid(), kind, definitionId, position, direction);
        history.Push(new ChangeSet(
            $"Colocar {kind} {definitionId}",
            () =>
            {
                if (document.Placements.All(value => value.Id != placement.Id)) document.Placements.Add(placement);
                dirty.Mark();
            },
            () =>
            {
                document.Placements.RemoveAll(value => value.Id == placement.Id);
                dirty.Mark();
            }));
        return placement;
    }

    public MapContentPlacementDefinition? FindPlacement(Guid id)
        => RequireDocument().Placements.FirstOrDefault(value => value.Id == id);

    public MapContentPlacementDefinition MovePlacement(Guid id, Vector2Data position)
    {
        var document = RequireDocument();
        EnsureInsideMap(document, position);
        var previous = document.Placements.FirstOrDefault(value => value.Id == id)
            ?? throw new KeyNotFoundException($"Placement inexistente: {id}.");
        if (previous.Position == position) return previous;

        var replacement = new MapContentPlacementDefinition(
            previous.Id,
            previous.Kind,
            previous.DefinitionId,
            position,
            previous.Direction,
            previous.Parameters);

        history.Push(new ChangeSet(
            $"Mover {previous.Kind} {previous.Id}",
            () => { ReplacePlacement(document, replacement); dirty.Mark(); },
            () => { ReplacePlacement(document, previous); dirty.Mark(); }));
        return replacement;
    }

    public bool RemovePlacement(Guid id)
    {
        var document = RequireDocument();
        var previous = document.Placements.FirstOrDefault(value => value.Id == id);
        if (previous is null) return false;

        history.Push(new ChangeSet(
            $"Borrar {previous.Kind} {previous.Id}",
            () => { document.Placements.RemoveAll(value => value.Id == id); dirty.Mark(); },
            () =>
            {
                if (document.Placements.All(value => value.Id != id)) document.Placements.Add(previous);
                dirty.Mark();
            }));
        return true;
    }

    public MapSpawnZoneDefinition AddSpawnZone(
        DefinitionId spawnTableId,
        MapShapeDefinition area,
        int maximumAliveOverride = 0)
    {
        var document = RequireDocument();
        if (!registry.TryGet<SpawnTableDefinition>(spawnTableId, out _))
            throw new InvalidOperationException($"{spawnTableId} no es una SpawnTableDefinition válida.");
        if (!document.Bounds.Contains(area.Center))
            throw new ArgumentOutOfRangeException(nameof(area), "El centro de la zona debe estar dentro del mapa.");

        var zone = new MapSpawnZoneDefinition(Guid.NewGuid(), spawnTableId, area, maximumAliveOverride);
        history.Push(new ChangeSet(
            $"Crear SpawnZone {spawnTableId}",
            () =>
            {
                if (document.SpawnZones.All(value => value.Id != zone.Id)) document.SpawnZones.Add(zone);
                dirty.Mark();
            },
            () =>
            {
                document.SpawnZones.RemoveAll(value => value.Id == zone.Id);
                dirty.Mark();
            }));
        return zone;
    }

    public MapSpawnZoneDefinition? FindSpawnZone(Guid id)
        => RequireDocument().SpawnZones.FirstOrDefault(value => value.Id == id);

    public bool RemoveSpawnZone(Guid id)
    {
        var document = RequireDocument();
        var previous = document.SpawnZones.FirstOrDefault(value => value.Id == id);
        if (previous is null) return false;

        history.Push(new ChangeSet(
            $"Borrar SpawnZone {id}",
            () => { document.SpawnZones.RemoveAll(value => value.Id == id); dirty.Mark(); },
            () =>
            {
                if (document.SpawnZones.All(value => value.Id != id)) document.SpawnZones.Add(previous);
                dirty.Mark();
            }));
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

    private void ValidatePlacementDefinition(SpawnEntityKind kind, DefinitionId id)
    {
        var valid = kind switch
        {
            SpawnEntityKind.Mob => registry.TryGet<MobDefinition>(id, out _),
            SpawnEntityKind.Npc => registry.TryGet<NpcDefinition>(id, out _),
            SpawnEntityKind.Resource => registry.TryGet<ResourceDefinition>(id, out _),
            _ => false
        };
        if (!valid)
            throw new InvalidOperationException($"{id} no es una Definition válida para placement {kind}.");
    }

    private static void EnsureInsideMap(MapDocument document, Vector2Data position)
    {
        if (!document.Bounds.Contains(position))
            throw new ArgumentOutOfRangeException(nameof(position), "El placement debe quedar dentro de los límites del mapa.");
    }

    private static void ReplacePlacement(MapDocument document, MapContentPlacementDefinition replacement)
    {
        var index = document.Placements.FindIndex(value => value.Id == replacement.Id);
        if (index < 0) throw new KeyNotFoundException($"Placement inexistente: {replacement.Id}.");
        document.Placements[index] = replacement;
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
            source.Parameters,
            source.Band);
}
