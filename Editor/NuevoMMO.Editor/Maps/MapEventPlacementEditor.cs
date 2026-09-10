using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Vista de eventos colocados en el mapa actual. Los eventos siguen siendo EventDefinitions
/// globalmente registradas; el mapa no duplica su contenido.
/// </summary>
public sealed class MapEventPlacementEditor
{
    private readonly EventDefinitionEditor events;
    private DefinitionId? mapId;

    public MapEventPlacementEditor(DefinitionRegistry registry)
        => events = new EventDefinitionEditor(registry);

    public void Bind(DefinitionId mapDefinitionId)
    {
        if (mapDefinitionId.IsEmpty) throw new ArgumentException("MapDefinitionId vacío.", nameof(mapDefinitionId));
        mapId = mapDefinitionId;
    }

    public IReadOnlyList<EventDefinition> List()
    {
        var currentMap = RequireMap();
        return events.List()
            .Where(evt => evt.Scope == EventScope.Map && evt.Placement?.MapId == currentMap)
            .OrderBy(evt => evt.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public EventDefinition Create(
        ContentKey key,
        string name,
        Vector2Data position,
        Direction direction = Direction.Down)
    {
        var rootListId = Guid.NewGuid();
        var page = new EventPageDefinition(
            Guid.NewGuid(),
            EventTrigger.Action,
            commandLists: new Dictionary<Guid, EventCommandDefinition[]> { [rootListId] = [] },
            rootCommandListId: rootListId);

        var definition = new EventDefinition(
            DefinitionId.New(),
            key,
            name,
            string.Empty,
            true,
            1,
            null,
            EventScope.Map,
            new EventPlacementDefinition(RequireMap(), position, direction),
            [page]);

        events.Create(definition);
        return definition;
    }

    public EventDefinition Move(DefinitionId eventId, Vector2Data position, Direction? direction = null)
    {
        var current = events.Edit(eventId);
        if (current.Scope != EventScope.Map || current.Placement is null)
            throw new InvalidOperationException("El evento no es un evento colocado en mapa.");
        if (current.Placement.MapId != RequireMap())
            throw new InvalidOperationException("El evento pertenece a otro mapa.");

        var updated = Copy(
            current,
            new EventPlacementDefinition(
                current.Placement.MapId,
                position,
                direction ?? current.Placement.Direction));
        events.Save(updated);
        return updated;
    }

    public bool Delete(DefinitionId eventId)
    {
        var current = events.Edit(eventId);
        if (current.Scope != EventScope.Map || current.Placement?.MapId != RequireMap())
            throw new InvalidOperationException("El evento no pertenece al mapa actual.");
        return events.Delete(eventId);
    }

    private DefinitionId RequireMap()
        => mapId ?? throw new InvalidOperationException("No hay mapa enlazado al editor de eventos.");

    private static EventDefinition Copy(EventDefinition source, EventPlacementDefinition placement)
        => new(
            source.Id,
            source.Key,
            source.Name,
            source.Description,
            source.Enabled,
            source.Version,
            source.Tags,
            source.Scope,
            placement,
            source.Pages,
            source.Metadata);
}
