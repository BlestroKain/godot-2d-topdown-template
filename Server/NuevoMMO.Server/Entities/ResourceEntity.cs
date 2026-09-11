using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class ResourceEntity : Entity
{
    private readonly ContentKey availableVisualKey;
    private readonly ContentKey? exhaustedVisualKey;

    public ResourceEntity(EntityId id, DefinitionId definition, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (definition.IsEmpty) throw new ArgumentException("DefinitionId inválido.", nameof(definition));
        DefinitionId = definition;
        availableVisualKey = visualKey;
        MaxHealth = 1;
        Health = 1;
        Properties = [];
    }

    public ResourceEntity(
        EntityId id,
        ResourceDefinition definition,
        MapInstanceId mapInstance,
        Vector2Data position,
        float maxHealth,
        ItemPropertyValue[]? properties = null)
        : base(id, mapInstance, position, RequireDefinition(definition).VisualKey, definition.Name)
    {
        if (!float.IsFinite(maxHealth) || maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
        DefinitionId = definition.Id;
        Harvest = definition.Harvest;
        availableVisualKey = definition.VisualKey;
        exhaustedVisualKey = definition.ExhaustedVisualKey;
        MaxHealth = maxHealth;
        Health = maxHealth;
        Properties = properties?.ToArray() ?? [];
        if (Properties.Any(static value => value.PropertyId.IsEmpty || !float.IsFinite(value.Value)))
            throw new ArgumentException("Propiedades runtime inválidas.", nameof(properties));
        if (definition.Collision is { } collision) ConfigureCollision(collision.ToRuntime());
    }

    public DefinitionId DefinitionId { get; }
    public ResourceHarvestDefinition Harvest { get; } = new();
    public float Health { get; private set; }
    public float MaxHealth { get; }
    public bool IsAvailable => Health > 0;
    public long RespawnAtMilliseconds { get; private set; }
    public ItemPropertyValue[] Properties { get; }
    public bool BlocksMovement => IsAvailable ? Harvest.BlocksMovementWhileAvailable : Harvest.BlocksMovementWhileExhausted;

    public float ApplyHarvestDamage(float amount, long nowMilliseconds)
    {
        if (!float.IsFinite(amount) || amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (!IsAvailable) return 0;
        var applied = Math.Min(Health, amount);
        Health -= applied;
        if (Health <= 0)
        {
            Health = 0;
            RespawnAtMilliseconds = Harvest.RespawnMilliseconds <= 0 ? 0 : checked(nowMilliseconds + Harvest.RespawnMilliseconds);
            if (exhaustedVisualKey is { } exhausted) SetVisualKey(exhausted);
        }
        return applied;
    }

    public bool TryRespawn(long nowMilliseconds)
    {
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (IsAvailable || RespawnAtMilliseconds == 0 || nowMilliseconds < RespawnAtMilliseconds) return false;
        Health = MaxHealth; RespawnAtMilliseconds = 0; SetVisualKey(availableVisualKey); return true;
    }

    public void ForceRespawn() { Health = MaxHealth; RespawnAtMilliseconds = 0; SetVisualKey(availableVisualKey); }
    public override EntityState ToState() => new ResourceState(Id, DefinitionId, MapInstanceId, Position, Direction, VisualKey, DisplayName);
    private static ResourceDefinition RequireDefinition(ResourceDefinition? definition) => definition ?? throw new ArgumentNullException(nameof(definition));
}
