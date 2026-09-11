namespace NuevoMMO.Core;

public sealed record ResourceState(EntityId Id, DefinitionId ResourceDefinition, MapInstanceId MapInstance,
    Vector2Data Position, Direction Facing, ContentKey VisualKey, string DisplayName)
    : EntityState(Id, EntityKind.Resource, ResourceDefinition, MapInstance, Position, default, Facing, VisualKey, DisplayName);
