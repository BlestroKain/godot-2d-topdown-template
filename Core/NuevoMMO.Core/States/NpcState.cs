namespace NuevoMMO.Core;

public sealed record NpcState(EntityId Id, DefinitionId NpcDefinition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, Direction Facing, ContentKey VisualKey, string DisplayName)
    : LivingEntityState(Id, EntityKind.Npc, NpcDefinition, MapInstance, Position, Velocity, Facing, VisualKey, DisplayName);
