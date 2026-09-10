using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class ResourceNodeEditor
{
    private MapDocument? document;
    public IReadOnlyList<MapContentPlacementDefinition> Resources => RequireDocument().Placements
        .Where(static placement => placement.Kind == SpawnEntityKind.Resource)
        .ToArray();

    public void Bind(MapDocument map) => document = map ?? throw new ArgumentNullException(nameof(map));

    public MapContentPlacementDefinition Add(
        DefinitionId resourceId,
        Vector2Data position,
        Direction direction = Direction.Down,
        Dictionary<string, float>? parameters = null)
    {
        var placement = new MapContentPlacementDefinition(
            Guid.NewGuid(), SpawnEntityKind.Resource, resourceId, position, direction, parameters);
        RequireDocument().Placements.Add(placement);
        return placement;
    }

    public bool Remove(Guid id)
    {
        var document = RequireDocument();
        return document.Placements.RemoveAll(
            placement => placement.Id == id && placement.Kind == SpawnEntityKind.Resource) > 0;
    }

    private MapDocument RequireDocument() => document ?? throw new InvalidOperationException("No hay mapa abierto.");
}
