using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class Mob : LivingEntity
{
    public Mob(EntityId id, DefinitionId definition, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (definition.Value == Guid.Empty) throw new ArgumentException("DefinitionId inválido.", nameof(definition));
        DefinitionId = definition;
    }

    public DefinitionId DefinitionId { get; }

    public override EntityState ToState() => new MobState(Id, DefinitionId, MapInstanceId, Position, Velocity,
        Direction, VisualKey, DisplayName);
}
