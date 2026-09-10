using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class Projectile : Entity
{
    public Projectile(EntityId id, DefinitionId? definition, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
        => DefinitionId = definition;

    public DefinitionId? DefinitionId { get; }

    public override EntityState ToState() => new ProjectileState(Id, DefinitionId, MapInstanceId, Position, Velocity,
        Direction, VisualKey, DisplayName);
}
