namespace NuevoMMO.Core;

public record LivingEntityState(EntityId Id, EntityKind Kind, DefinitionId? Definition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, Direction Facing, ContentKey VisualKey, string DisplayName)
    : EntityState(Id, Kind, Definition, MapInstance, Position, Velocity, Facing, VisualKey, DisplayName);
