using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class Npc : LivingEntity
{
    public Npc(EntityId id, DefinitionId definition, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
        => DefinitionId = definition;

    public DefinitionId DefinitionId { get; }

    public override EntityState ToState() => new NpcState(Id, DefinitionId, MapInstanceId, Position, Velocity,
        Direction, VisualKey, DisplayName);
}
