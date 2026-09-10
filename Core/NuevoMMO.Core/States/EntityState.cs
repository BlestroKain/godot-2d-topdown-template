namespace NuevoMMO.Core;

public record EntityState(EntityId Id, EntityKind Kind, DefinitionId? Definition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, Direction Facing, ContentKey VisualKey, string DisplayName);
