namespace NuevoMMO.Core;

public sealed record ProjectileState(EntityId Id, DefinitionId? Definition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, Direction Facing, ContentKey VisualKey, string DisplayName)
    : EntityState(Id, EntityKind.Projectile, Definition, MapInstance, Position, Velocity, Facing, VisualKey, DisplayName);
