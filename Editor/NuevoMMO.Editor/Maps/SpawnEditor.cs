using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class SpawnEditor
{
    private MapDocument? document;
    public IReadOnlyList<MapSpawnZoneDefinition> Zones => RequireDocument().SpawnZones;
    public IReadOnlyList<MapContentPlacementDefinition> Placements => RequireDocument().Placements;

    public void Bind(MapDocument map) => document = map ?? throw new ArgumentNullException(nameof(map));

    public MapSpawnZoneDefinition AddZone(DefinitionId spawnTableId, MapShapeDefinition area, int maximumAliveOverride = 0)
    {
        var zone = new MapSpawnZoneDefinition(Guid.NewGuid(), spawnTableId, area, maximumAliveOverride);
        RequireDocument().SpawnZones.Add(zone);
        return zone;
    }

    public MapContentPlacementDefinition AddPlacement(
        SpawnEntityKind kind,
        DefinitionId definitionId,
        Vector2Data position,
        Direction direction = Direction.Down)
    {
        var placement = new MapContentPlacementDefinition(Guid.NewGuid(), kind, definitionId, position, direction);
        RequireDocument().Placements.Add(placement);
        return placement;
    }

    public bool RemoveZone(Guid id) => RequireDocument().SpawnZones.RemoveAll(zone => zone.Id == id) > 0;
    public bool RemovePlacement(Guid id) => RequireDocument().Placements.RemoveAll(placement => placement.Id == id) > 0;

    private MapDocument RequireDocument() => document ?? throw new InvalidOperationException("No hay mapa abierto.");
}
