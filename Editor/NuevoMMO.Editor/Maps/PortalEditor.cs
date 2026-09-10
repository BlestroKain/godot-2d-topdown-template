using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class PortalEditor
{
    private MapDocument? document;
    public IReadOnlyList<MapPortalDefinition> Portals => RequireDocument().Portals;

    public void Bind(MapDocument map) => document = map ?? throw new ArgumentNullException(nameof(map));

    public MapPortalDefinition Add(
        MapShapeDefinition triggerArea,
        DefinitionId destinationMapId,
        Vector2Data destination,
        Direction destinationDirection = Direction.Down,
        ConditionGroupDefinition? requirements = null,
        DefinitionId? eventId = null,
        ContentKey? transitionKey = null)
    {
        var portal = new MapPortalDefinition(
            Guid.NewGuid(), triggerArea, destinationMapId, destination,
            destinationDirection, requirements, eventId, transitionKey);
        RequireDocument().Portals.Add(portal);
        return portal;
    }

    public bool Remove(Guid id) => RequireDocument().Portals.RemoveAll(portal => portal.Id == id) > 0;

    private MapDocument RequireDocument() => document ?? throw new InvalidOperationException("No hay mapa abierto.");
}
