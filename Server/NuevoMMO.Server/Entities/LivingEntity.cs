using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public abstract class LivingEntity : Entity
{
    protected LivingEntity(EntityId id, MapInstanceId mapInstance, Vector2Data position, ContentKey visualKey,
        string displayName, int maxHealth = 100, int maxMana = 100)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (maxHealth < 1 || maxMana < 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
        MaxHealth = maxHealth;
        Health = maxHealth;
        MaxMana = maxMana;
        Mana = maxMana;
    }

    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int Mana { get; private set; }
    public int MaxMana { get; private set; }
    public StatBlock Stats { get; } = new();
    public EffectSet Effects { get; } = new();
    public CombatState CombatState { get; } = new();
    public MovementState MovementState { get; } = new();
    public bool IsAlive => Health > 0;

    public void ApplyDamage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!IsAlive) return;
        Health = Math.Max(0, Health - amount);
        if (Health == 0) Die();
    }

    public void ApplyHealing(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!IsAlive) return;
        Health = Math.Min(MaxHealth, Health + amount);
    }

    public void ConsumeMana(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Mana < amount) throw new InvalidOperationException("Maná insuficiente.");
        Mana -= amount;
    }

    public void RestoreMana(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Mana = Math.Min(MaxMana, Mana + amount);
    }

    public void Die()
    {
        Health = 0;
        CombatState.InCombat = false;
        CombatState.Target = null;
        MovementState.CanMove = false;
    }
}
