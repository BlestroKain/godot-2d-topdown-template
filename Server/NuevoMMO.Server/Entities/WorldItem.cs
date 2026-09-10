using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class WorldItem : Entity
{
    public WorldItem(EntityId id, DefinitionId definition, ItemInstanceId itemInstance, MapInstanceId mapInstance,
        Vector2Data position, ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        DefinitionId = definition;
        ItemInstanceId = itemInstance;
    }

    public DefinitionId DefinitionId { get; }
    public ItemInstanceId ItemInstanceId { get; }

    public override EntityState ToState() => new WorldItemState(Id, DefinitionId, ItemInstanceId, MapInstanceId,
        Position, Direction, VisualKey, DisplayName);
}
