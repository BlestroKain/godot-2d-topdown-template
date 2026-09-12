namespace NuevoMMO.Core;

public sealed record MobState(EntityId Id, DefinitionId MobDefinition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, Direction Facing, ContentKey VisualKey, string DisplayName)
    : LivingEntityState(Id, EntityKind.Mob, MobDefinition, MapInstance, Position, Velocity, Facing, VisualKey, DisplayName);
