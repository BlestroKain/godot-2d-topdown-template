using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class ResourceEntity : Entity
{
    public ResourceEntity(EntityId id, DefinitionId definition, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
        => DefinitionId = definition;

    public DefinitionId DefinitionId { get; }

    public override EntityState ToState() => new ResourceState(Id, DefinitionId, MapInstanceId, Position, Direction,
        VisualKey, DisplayName);
}
