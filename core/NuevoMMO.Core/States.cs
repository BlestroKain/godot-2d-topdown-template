namespace NuevoMMO.Core;

public enum EntityKind : byte { Player, Mob, Npc, Resource, Projectile, WorldItem, InteractiveObject }

public record EntityState(EntityId Id, EntityKind Kind, DefinitionId? Definition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, ContentKey VisualKey, string DisplayName);
public record LivingEntityState(EntityId Id, EntityKind Kind, DefinitionId? Definition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, ContentKey VisualKey, string DisplayName)
    : EntityState(Id, Kind, Definition, MapInstance, Position, Velocity, VisualKey, DisplayName);
public sealed record PlayerState(EntityId Id, CharacterId Character, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, ContentKey VisualKey, string DisplayName)
    : LivingEntityState(Id, EntityKind.Player, null, MapInstance, Position, Velocity, VisualKey, DisplayName);
public sealed record MobState(EntityId Id, DefinitionId MobDefinition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, ContentKey VisualKey, string DisplayName)
    : LivingEntityState(Id, EntityKind.Mob, MobDefinition, MapInstance, Position, Velocity, VisualKey, DisplayName);
public sealed record NpcState(EntityId Id, DefinitionId NpcDefinition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, ContentKey VisualKey, string DisplayName)
    : LivingEntityState(Id, EntityKind.Npc, NpcDefinition, MapInstance, Position, Velocity, VisualKey, DisplayName);
public sealed record ResourceState(EntityId Id, DefinitionId ResourceDefinition, MapInstanceId MapInstance,
    Vector2Data Position, ContentKey VisualKey, string DisplayName)
    : EntityState(Id, EntityKind.Resource, ResourceDefinition, MapInstance, Position, default, VisualKey, DisplayName);
public sealed record ProjectileState(EntityId Id, DefinitionId? Definition, MapInstanceId MapInstance,
    Vector2Data Position, Vector2Data Velocity, ContentKey VisualKey, string DisplayName)
    : EntityState(Id, EntityKind.Projectile, Definition, MapInstance, Position, Velocity, VisualKey, DisplayName);
public sealed record WorldItemState(EntityId Id, DefinitionId ItemDefinition, ItemInstanceId ItemInstance,
    MapInstanceId MapInstance, Vector2Data Position, ContentKey VisualKey, string DisplayName)
    : EntityState(Id, EntityKind.WorldItem, ItemDefinition, MapInstance, Position, default, VisualKey, DisplayName);
