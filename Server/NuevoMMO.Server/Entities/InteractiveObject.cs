using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class InteractiveObject : Entity
{
    public InteractiveObject(EntityId id, DefinitionId? definition, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
        => DefinitionId = definition;

    public DefinitionId? DefinitionId { get; }

    public override EntityState ToState() => new(Id, EntityKind.InteractiveObject, DefinitionId, MapInstanceId,
        Position, Velocity, Direction, VisualKey, DisplayName);
}
