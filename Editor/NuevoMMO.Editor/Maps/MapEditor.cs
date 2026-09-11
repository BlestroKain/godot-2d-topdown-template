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

    public bool EraseTile(Vector2IntData cell) => EraseTiles([cell]) > 0;

    public int PaintTiles(IReadOnlyList<Vector2IntData> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        var unique = UniqueInside(cells);
        if (unique.Count == 0) return 0;

        var previous = Layers.Active();
        var remaining = previous.Tiles.Where(tile => !unique.Contains(tile.Cell)).ToList();
        foreach (var cell in unique.OrderBy(static cell => cell.Y).ThenBy(static cell => cell.X))
            remaining.Add(Palette.CreatePlacement(cell));
        var replacement = CopyLayer(previous, remaining.ToArray());

        history.Push(new ChangeSet(
            unique.Count == 1 ? $"Pintar tile {unique.First()}" : $"Pintar {unique.Count} tiles",
            () => { Layers.Replace(replacement); dirty.Mark(); },
            () => { Layers.Replace(previous); dirty.Mark(); }));
        return unique.Count;
    }

    public int EraseTiles(IReadOnlyList<Vector2IntData> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        var unique = UniqueInside(cells);
        var previous = Layers.Active();
        var remaining = previous.Tiles.Where(tile => !unique.Contains(tile.Cell)).ToArray();
        if (remaining.Length == previous.Tiles.Length) return 0;
        var removed = previous.Tiles.Length - remaining.Length;
        var replacement = CopyLayer(previous, remaining);

        history.Push(new ChangeSet(
            removed == 1 ? "Borrar tile" : $"Borrar {removed} tiles",
            () => { Layers.Replace(replacement); dirty.Mark(); },
            () => { Layers.Replace(previous); dirty.Mark(); }));
        return removed;
    }

    public int PaintRect(Vector2IntData a, Vector2IntData b) => PaintTiles(CellsInRect(a, b));

    public int Fill(Vector2IntData start)
    {
        var document = RequireDocument();
        if (!IsCellInside(document, start)) return 0;
        var layer = Layers.Active();
        var sample = TileAt(layer, start);
        var fill = new HashSet<Vector2IntData>();
        var queue = new Queue<Vector2IntData>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (!fill.Add(cell) || !IsCellInside(document, cell)) continue;
            if (!SameTile(TileAt(layer, cell), sample)) { fill.Remove(cell); continue; }
            queue.Enqueue(new(cell.X - 1, cell.Y));
            queue.Enqueue(new(cell.X + 1, cell.Y));
            queue.Enqueue(new(cell.X, cell.Y - 1));
            queue.Enqueue(new(cell.X, cell.Y + 1));
        }

        return PaintTiles(fill.ToArray());
    }

    public MapTileClipboard Copy(IReadOnlyCollection<Vector2IntData> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        var layer = Layers.Active();
        var tiles = layer.Tiles.Where(tile => cells.Contains(tile.Cell)).ToArray();
        if (tiles.Length == 0) throw new InvalidOperationException("La selección no contiene tiles.");
        var originX = tiles.Min(static tile => tile.Cell.X);
        var originY = tiles.Min(static tile => tile.Cell.Y);
        return new MapTileClipboard(new(originX, originY), tiles);
    }

    public int Paste(Vector2IntData origin, MapTileClipboard clipboard)
    {
        ArgumentNullException.ThrowIfNull(clipboard);
        var document = RequireDocument();
        var offsetX = origin.X - clipboard.Origin.X;
        var offsetY = origin.Y - clipboard.Origin.Y;
        var previous = Layers.Active();
        var remaining = previous.Tiles.ToList();
        var pasted = 0;
        foreach (var tile in clipboard.Tiles)
        {
            var cell = new Vector2IntData(tile.Cell.X + offsetX, tile.Cell.Y + offsetY);
            if (!IsCellInside(document, cell)) continue;
            remaining.RemoveAll(existing => existing.Cell == cell);
            remaining.Add(new MapTilePlacementDefinition(
                cell,
                tile.TilesetKey,
                tile.AtlasCell,
                tile.Alternative,
                tile.RotationQuarterTurns,
                tile.FlipHorizontal,
                tile.FlipVertical,
                tile.Autotile));
            pasted++;
        }

        if (pasted == 0) return 0;
        var replacement = CopyLayer(previous, remaining.ToArray());
        history.Push(new ChangeSet(
            $"Pegar {pasted} tiles",
            () => { Layers.Replace(replacement); dirty.Mark(); },
            () => { Layers.Replace(previous); dirty.Mark(); }));
        return pasted;
    }

    public MapSpawnZoneDefinition PlaceSpawnZone(DefinitionId spawnTableId, MapShapeDefinition area, int maximumAliveOverride = 0)
        => AddSpawnZone(spawnTableId, area, maximumAliveOverride);

    public MapPortalDefinition PlacePortal(MapShapeDefinition trigger, DefinitionId destinationMapId, Vector2Data destination)
    {
        var document = RequireDocument();
        var portal = new MapPortalDefinition(Guid.NewGuid(), trigger, destinationMapId, destination);
        history.Push(new ChangeSet(
            "Colocar portal",
            () => { if (!document.Portals.Contains(portal)) document.Portals.Add(portal); dirty.Mark(); },
            () => { document.Portals.RemoveAll(value => value.Id == portal.Id); dirty.Mark(); }));
        return portal;
    }

    public MapRegionDefinition PlaceRegion(string key, string name, MapShapeDefinition area)
    {
        var document = RequireDocument();
        var region = new MapRegionDefinition(Guid.NewGuid(), key, name, area);
        history.Push(new ChangeSet(
            "Colocar región",
            () => { if (!document.Regions.Contains(region)) document.Regions.Add(region); dirty.Mark(); },
            () => { document.Regions.RemoveAll(value => value.Id == region.Id); dirty.Mark(); }));
        return region;
    }

    public MapLightDefinition PlaceLight(Vector2Data position, float radius = 96)
    {
        var document = RequireDocument();
        var light = new MapLightDefinition(Guid.NewGuid(), position, radius);
        history.Push(new ChangeSet(
            "Colocar luz",
            () => { if (!document.Lights.Contains(light)) document.Lights.Add(light); dirty.Mark(); },
            () => { document.Lights.RemoveAll(value => value.Id == light.Id); dirty.Mark(); }));
        return light;
    }

    public EventDefinition PlaceEvent(Vector2Data position)
    {
        var key = new ContentKey($"events.{Guid.NewGuid():N}");
        var created = Events.Create(key, "Evento", position);
        dirty.Mark();
        return created;
    }

    public IReadOnlyList<Vector2IntData> CellsInRect(Vector2IntData a, Vector2IntData b)
    {
        var document = RequireDocument();
        var minX = Math.Min(a.X, b.X);
        var maxX = Math.Max(a.X, b.X);
        var minY = Math.Min(a.Y, b.Y);
        var maxY = Math.Max(a.Y, b.Y);
        var cells = new List<Vector2IntData>();
        for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var cell = new Vector2IntData(x, y);
                if (IsCellInside(document, cell)) cells.Add(cell);
            }

        return cells;
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

    private HashSet<Vector2IntData> UniqueInside(IEnumerable<Vector2IntData> cells)
    {
        var document = RequireDocument();
        return cells.Where(cell => IsCellInside(document, cell)).ToHashSet();
    }

    private static bool IsCellInside(MapDocument map, Vector2IntData cell)
    {
        var width = Math.Max(1, (int)Math.Ceiling(map.Bounds.Width / map.TileSize.X));
        var height = Math.Max(1, (int)Math.Ceiling(map.Bounds.Height / map.TileSize.Y));
        return cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;
    }

    private static MapTilePlacementDefinition? TileAt(MapLayerDefinition layer, Vector2IntData cell)
        => layer.Tiles.FirstOrDefault(tile => tile.Cell == cell);

    private static bool SameTile(MapTilePlacementDefinition? left, MapTilePlacementDefinition? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.TilesetKey == right.TilesetKey
            && left.AtlasCell == right.AtlasCell
            && left.Alternative == right.Alternative
            && left.Autotile == right.Autotile;
    }

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
