using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public abstract class LivingEntity : Entity
{
    protected LivingEntity(EntityId id, MapInstanceId mapInstance, Vector2Data position, ContentKey visualKey,
        string displayName, int maxHealth = 100, int maxMana = 100)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (maxHealth < 1) throw new ArgumentOutOfRangeException(nameof(maxHealth));
        if (maxMana < 0) throw new ArgumentOutOfRangeException(nameof(maxMana));
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
    public float HealthPercent => MaxHealth <= 0 ? 0 : Health * 100f / MaxHealth;
    public float ManaPercent => MaxMana <= 0 ? 0 : Mana * 100f / MaxMana;

    public void ApplyDamage(int amount) => TakeDamage(amount);

    public int TakeDamage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!IsAlive || amount == 0) return 0;
        var applied = Math.Min(Health, amount);
        Health -= applied;
        if (Health == 0) Die();
        return applied;
    }

    public void ApplyHealing(int amount) => Heal(amount);

    public int Heal(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!IsAlive || amount == 0) return 0;
        var applied = Math.Min(amount, MaxHealth - Health);
        Health += applied;
        return applied;
    }

    public bool TryConsumeMana(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Mana < amount) return false;
        Mana -= amount;
        return true;
    }

    public void ConsumeMana(int amount)
    {
        if (!TryConsumeMana(amount)) throw new InvalidOperationException("Maná insuficiente.");
    }

    public int RestoreMana(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount == 0 || MaxMana == 0) return 0;
        var applied = Math.Min(amount, MaxMana - Mana);
        Mana += applied;
        return applied;
    }

    public void SetMaximumVitals(int maxHealth, int maxMana, bool refill = true)
    {
        if (maxHealth < 1) throw new ArgumentOutOfRangeException(nameof(maxHealth));
        if (maxMana < 0) throw new ArgumentOutOfRangeException(nameof(maxMana));
        MaxHealth = maxHealth;
        MaxMana = maxMana;
        if (refill)
        {
            Health = maxHealth;
            Mana = maxMana;
        }
        else
        {
            Health = Math.Min(Health, maxHealth);
            Mana = Math.Min(Mana, maxMana);
        }
    }

    public void SetVitals(int health, int mana)
    {
        if (health < 0 || health > MaxHealth) throw new ArgumentOutOfRangeException(nameof(health));
        if (mana < 0 || mana > MaxMana) throw new ArgumentOutOfRangeException(nameof(mana));
        Health = health;
        Mana = mana;
        if (Health == 0) Die();
    }

    public void Revive(int health, int mana = 0)
    {
        if (health < 1 || health > MaxHealth) throw new ArgumentOutOfRangeException(nameof(health));
        if (mana < 0 || mana > MaxMana) throw new ArgumentOutOfRangeException(nameof(mana));
        Health = health;
        Mana = mana;
        MovementState.CanMove = true;
        CombatState.InCombat = false;
        CombatState.Target = null;
    }

    public void EnterCombat(EntityId? target = null)
    {
        if (!IsAlive) return;
        CombatState.InCombat = true;
        if (target is { } value && value.Value > 0) CombatState.Target = value;
    }

    public void LeaveCombat()
    {
        CombatState.InCombat = false;
        CombatState.Target = null;
    }

    public void Die()
    {
        if (Health == 0 && !MovementState.CanMove) return;
        Health = 0;
        LeaveCombat();
        MovementState.CanMove = false;
    }

    /// <summary>
    /// Carga los stats/vitales base de CreatureCombatDefinition en el estado runtime.
    /// Conservamos StatBlock actual (entero) y redondeamos explícitamente los valores de definición.
    /// </summary>
    protected void ApplyCombatProfile(CreatureCombatDefinition profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        foreach (var pair in profile.Stats) WriteStat(Stats, pair.Key, pair.Value);

        var maxHealth = profile.MaxVitals.TryGetValue(VitalId.Health, out var hp)
            ? ToVital(hp, minimum: 1)
            : MaxHealth;
        var maxMana = profile.MaxVitals.TryGetValue(VitalId.Mana, out var mp)
            ? ToVital(mp, minimum: 0)
            : MaxMana;
        SetMaximumVitals(maxHealth, maxMana, refill: true);
    }

    private static int ToVital(float value, int minimum)
    {
        if (!float.IsFinite(value) || value < minimum) throw new ArgumentOutOfRangeException(nameof(value));
        if (value > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
        return Math.Max(minimum, (int)MathF.Round(value, MidpointRounding.AwayFromZero));
    }

    private static void WriteStat(StatBlock stats, StatId stat, float value)
    {
        if (!float.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value));
        var rounded = (int)MathF.Round(value, MidpointRounding.AwayFromZero);
        switch (stat)
        {
            case StatId.Strength: stats.Primary.Strength = rounded; break;
            case StatId.Intelligence: stats.Primary.Intelligence = rounded; break;
            case StatId.Agility: stats.Primary.Agility = rounded; break;
            case StatId.Spirit: stats.Primary.Spirit = rounded; break;
            case StatId.Vitality: stats.Primary.Vitality = rounded; break;
            case StatId.Luck: stats.Secondary.Luck = rounded; break;
            case StatId.ResistEarth: stats.Resistances.Earth = rounded; break;
            case StatId.ResistFire: stats.Resistances.Fire = rounded; break;
            case StatId.ResistAir: stats.Resistances.Air = rounded; break;
            case StatId.ResistWater: stats.Resistances.Water = rounded; break;
            case StatId.ResistNeutral: stats.Resistances.Neutral = rounded; break;
            default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
        }
    }
}
