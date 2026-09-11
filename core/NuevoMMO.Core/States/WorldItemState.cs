namespace NuevoMMO.Core;

public sealed record WorldItemState(EntityId Id, DefinitionId ItemDefinition, ItemInstanceId ItemInstance,
    MapInstanceId MapInstance, Vector2Data Position, Direction Facing, ContentKey VisualKey, string DisplayName)
    : EntityState(Id, EntityKind.WorldItem, ItemDefinition, MapInstance, Position, default, Facing, VisualKey, DisplayName);
