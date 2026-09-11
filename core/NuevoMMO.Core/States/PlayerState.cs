namespace NuevoMMO.Core;

public sealed record PlayerState(EntityId Id, CharacterId Character, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, Direction Facing, ContentKey VisualKey, string DisplayName)
    : LivingEntityState(Id, EntityKind.Player, null, MapInstance, Position, Velocity, Facing, VisualKey, DisplayName);
