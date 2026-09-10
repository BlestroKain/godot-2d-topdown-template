using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public abstract class Entity(EntityId id, MapInstanceId mapInstance, Vector2Data position, ContentKey visualKey, string displayName)
{
    public EntityId Id { get; } = id.Value > 0 ? id : throw new ArgumentException("EntityId inválido.");
    public MapInstanceId MapInstance { get; } = mapInstance.Value > 0 ? mapInstance : throw new ArgumentException("MapInstanceId inválido.");
    public Vector2Data Position { get; private set; } = position.IsFinite ? position : throw new ArgumentException("Posición inválida.");
    public Vector2Data Velocity { get; private set; }
    public ContentKey VisualKey { get; } = visualKey;
    public string DisplayName { get; } = !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : throw new ArgumentException("Nombre vacío.");
    public void MoveTo(Vector2Data position, Vector2Data velocity)
    {
        if (!position.IsFinite || !velocity.IsFinite) throw new ArgumentException("Movimiento inválido.");
        Position = position; Velocity = velocity;
    }
    public abstract EntityState ToState();
}

public abstract class LivingEntity(EntityId id, MapInstanceId mapInstance, Vector2Data position, ContentKey visualKey, string displayName)
    : Entity(id, mapInstance, position, visualKey, displayName);

public sealed class Player(EntityId id, CharacterId character, MapInstanceId mapInstance, Vector2Data position,
    ContentKey visualKey, string displayName) : LivingEntity(id, mapInstance, position, visualKey, displayName)
{
    public CharacterId Character { get; } = character.Value != Guid.Empty ? character : throw new ArgumentException("CharacterId inválido.");
    public MovementInputBuffer Inputs { get; } = new();
    public bool DirtyPosition { get; private set; }
    public void ApplyMovement(Vector2Data position, Vector2Data velocity) { MoveTo(position, velocity); if (velocity.LengthSquared > 0) DirtyPosition = true; }
    public void MarkSaved() => DirtyPosition = false;
    public override EntityState ToState() => new PlayerState(Id, Character, MapInstance, Position, Velocity, VisualKey, DisplayName);
}

public sealed class Mob(EntityId id, DefinitionId definition, MapInstanceId mapInstance, Vector2Data position,
    ContentKey visualKey, string displayName) : LivingEntity(id, mapInstance, position, visualKey, displayName)
{
    public DefinitionId Definition { get; } = definition.Value != Guid.Empty ? definition : throw new ArgumentException("DefinitionId inválido.");
    public override EntityState ToState() => new MobState(Id, Definition, MapInstance, Position, Velocity, VisualKey, DisplayName);
}

public sealed class Npc(EntityId id, DefinitionId definition, MapInstanceId mapInstance, Vector2Data position,
    ContentKey visualKey, string displayName) : LivingEntity(id, mapInstance, position, visualKey, displayName)
{
    public DefinitionId Definition { get; } = definition;
    public override EntityState ToState() => new NpcState(Id, Definition, MapInstance, Position, Velocity, VisualKey, DisplayName);
}

public sealed class ResourceEntity(EntityId id, DefinitionId definition, MapInstanceId mapInstance, Vector2Data position,
    ContentKey visualKey, string displayName) : Entity(id, mapInstance, position, visualKey, displayName)
{
    public DefinitionId Definition { get; } = definition;
    public override EntityState ToState() => new ResourceState(Id, Definition, MapInstance, Position, VisualKey, DisplayName);
}

public sealed class Projectile(EntityId id, DefinitionId? definition, MapInstanceId mapInstance, Vector2Data position,
    ContentKey visualKey, string displayName) : Entity(id, mapInstance, position, visualKey, displayName)
{
    public DefinitionId? Definition { get; } = definition;
    public override EntityState ToState() => new ProjectileState(Id, Definition, MapInstance, Position, Velocity, VisualKey, DisplayName);
}

public sealed class WorldItem(EntityId id, DefinitionId definition, ItemInstanceId itemInstance, MapInstanceId mapInstance,
    Vector2Data position, ContentKey visualKey, string displayName) : Entity(id, mapInstance, position, visualKey, displayName)
{
    public DefinitionId Definition { get; } = definition;
    public ItemInstanceId ItemInstance { get; } = itemInstance;
    public override EntityState ToState() => new WorldItemState(Id, Definition, ItemInstance, MapInstance, Position, VisualKey, DisplayName);
}

public sealed class InteractiveObject(EntityId id, DefinitionId? definition, MapInstanceId mapInstance, Vector2Data position,
    ContentKey visualKey, string displayName) : Entity(id, mapInstance, position, visualKey, displayName)
{
    public DefinitionId? Definition { get; } = definition;
    public override EntityState ToState() => new(Id, EntityKind.InteractiveObject, Definition, MapInstance, Position, Velocity, VisualKey, DisplayName);
}
