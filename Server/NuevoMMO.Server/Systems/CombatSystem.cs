using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public sealed record DamageResult(
    EntityId? Attacker,
    EntityId Target,
    Element Element,
    float RawDamage,
    float ResistancePercent,
    bool Critical,
    int AppliedDamage,
    bool Killed);

public sealed record HealingResult(
    EntityId? Source,
    EntityId Target,
    int Requested,
    int Applied);

/// <summary>
/// Núcleo autoritativo de combate. Resuelve escalado, crítico, resistencia y aplicación de daño.
/// Targeting, alcance, LoS y relaciones son responsabilidades de los sistemas de técnica/IA.
/// </summary>
public sealed class CombatSystem
{
    public CombatSystem(CombatTelemetry? telemetry = null)
        => Telemetry = telemetry ?? new CombatTelemetry();

    public CombatTelemetry Telemetry { get; }

    public DamageResult ExecuteMobBasicAttack(
        Mob attacker,
        LivingEntity target,
        long nowMilliseconds,
        float? criticalRoll = null)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);
        if (!attacker.IsAlive) throw new InvalidOperationException("El atacante está muerto.");
        if (!target.IsAlive) throw new InvalidOperationException("El objetivo está muerto.");
        if (attacker.MapInstanceId != target.MapInstanceId)
            throw new InvalidOperationException("Atacante y objetivo no están en la misma instancia.");
        if (!attacker.CanBasicAttack(nowMilliseconds))
            throw new InvalidOperationException("El ataque básico todavía está en cooldown.");

        var profile = attacker.Combat;
        var raw = CalculateScaledAmount(profile.BaseDamage, profile.Scaling, attacker.Stats);
        var critical = RollCritical(profile.CriticalChancePercent, criticalRoll);
        if (critical) raw *= profile.CriticalMultiplier;

        attacker.MarkBasicAttack(nowMilliseconds);
        return ApplyDamage(attacker, target, raw, profile.BasicAttackElement, critical, nowMilliseconds);
    }

    /// <summary>
    /// Ataque de prueba/data-driven donde DamageType y ScalingAttribute son independientes.
    /// Útil para técnicas elementales y neutral sin convertir Neutral en STR implícitamente.
    /// </summary>
    public DamageResult ExecuteAttributeDamage(
        LivingEntity attacker,
        LivingEntity target,
        AttributeDamageFormula formula,
        long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(formula);
        if (!attacker.IsAlive || !target.IsAlive)
            throw new InvalidOperationException("Atacante y objetivo deben estar vivos.");
        if (attacker.MapInstanceId != target.MapInstanceId)
            throw new InvalidOperationException("Atacante y objetivo no están en la misma instancia.");

        var resistance = Resistance(target, formula.DamageType);
        var resolved = CanonicalDamageRules.Resolve(formula, attacker.Stats.Primary, resistance);
        return ApplyDamage(
            attacker,
            target,
            resolved.RawDamage,
            formula.DamageType,
            resolved.Critical,
            nowMilliseconds,
            usePveResistanceCap: true);
    }

    public DamageResult ApplyDamage(
        LivingEntity? attacker,
        LivingEntity target,
        float rawDamage,
        Element element,
        bool critical = false,
        long? nowMilliseconds = null,
        bool usePveResistanceCap = false)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!float.IsFinite(rawDamage) || rawDamage < 0) throw new ArgumentOutOfRangeException(nameof(rawDamage));
        if (!target.IsAlive)
            return new DamageResult(attacker?.Id, target.Id, element, rawDamage, Resistance(target, element), critical, 0, false);
        if (attacker is not null && attacker.MapInstanceId != target.MapInstanceId)
            throw new InvalidOperationException("Atacante y objetivo no están en la misma instancia.");

        var resistance = Resistance(target, element);
        var effectiveResistance = usePveResistanceCap
            ? Math.Min(resistance, CanonicalDamageRules.PositivePveResistanceCap)
            : resistance;
        var multiplier = Math.Max(0f, 1f - effectiveResistance / 100f);
        var afterResistance = rawDamage * multiplier;
        if (!float.IsFinite(afterResistance) || afterResistance > int.MaxValue)
            throw new OverflowException("El daño resultante excede el rango soportado.");

        var requested = Math.Max(0, (int)MathF.Round(afterResistance, MidpointRounding.AwayFromZero));
        var wasAlive = target.IsAlive;
        var applied = target.TakeDamage(requested);
        var killed = wasAlive && !target.IsAlive;

        if (attacker is not null && applied > 0)
        {
            attacker.EnterCombat(target.Id);
            if (target.IsAlive) target.EnterCombat(attacker.Id);
            if (target is Mob mob)
            {
                mob.AddThreat(attacker.Id, applied);
                if (mob.CombatState.Target is null) mob.BeginAggro(attacker.Id);
            }
        }

        var result = new DamageResult(attacker?.Id, target.Id, element, rawDamage, effectiveResistance, critical, applied, killed);
        if (target is Mob targetMob &&
            targetMob.Combat.Parameters.GetValueOrDefault("track_damage_telemetry") > 0)
        {
            Telemetry.Record(target.Id, result, nowMilliseconds ?? Environment.TickCount64);
        }
        return result;
    }

    public HealingResult ApplyHealing(LivingEntity? source, LivingEntity target, int amount)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (source is not null && source.MapInstanceId != target.MapInstanceId)
            throw new InvalidOperationException("Fuente y objetivo no están en la misma instancia.");
        var applied = target.Heal(amount);
        return new HealingResult(source?.Id, target.Id, amount, applied);
    }

    /// <summary>
    /// Interpreta los valores de Scaling como porcentajes del stat: 100 = 100% del stat,
    /// 4000 = 4000%, igual al lenguaje de diseño usado por las técnicas del proyecto.
    /// </summary>
    public static float CalculateScaledAmount(
        float baseAmount,
        IReadOnlyDictionary<StatId, float> scaling,
        StatBlock stats)
    {
        if (!float.IsFinite(baseAmount)) throw new ArgumentOutOfRangeException(nameof(baseAmount));
        ArgumentNullException.ThrowIfNull(scaling);
        ArgumentNullException.ThrowIfNull(stats);

        var result = baseAmount;
        foreach (var pair in scaling)
            result += ReadStat(stats, pair.Key) * (pair.Value / 100f);
        if (!float.IsFinite(result)) throw new OverflowException("El escalado produjo un valor no finito.");
        return Math.Max(0, result);
    }

    public static float Resistance(LivingEntity target, Element element)
    {
        ArgumentNullException.ThrowIfNull(target);
        return element switch
        {
            Element.Earth => target.Stats.Resistances.Earth,
            Element.Fire => target.Stats.Resistances.Fire,
            Element.Air => target.Stats.Resistances.Air,
            Element.Water => target.Stats.Resistances.Water,
            Element.Neutral => target.Stats.Resistances.Neutral,
            _ => throw new ArgumentOutOfRangeException(nameof(element), element, null)
        };
    }

    private static bool RollCritical(float chancePercent, float? suppliedRoll)
    {
        if (!float.IsFinite(chancePercent) || chancePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(chancePercent));
        if (chancePercent <= 0) return false;
        if (chancePercent >= 100) return true;

        var roll = suppliedRoll ?? Random.Shared.NextSingle();
        if (!float.IsFinite(roll) || roll is < 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(suppliedRoll), "El roll crítico debe estar en [0,1)." );
        return roll < chancePercent / 100f;
    }

    private static float ReadStat(StatBlock stats, StatId stat)
        => stat switch
        {
            StatId.Strength => stats.Primary.Strength,
            StatId.Intelligence => stats.Primary.Intelligence,
            StatId.Agility => stats.Primary.Agility,
            StatId.Spirit => stats.Primary.Spirit,
            StatId.Vitality => stats.Primary.Vitality,
            StatId.Luck => stats.Secondary.Luck,
            StatId.ResistEarth => stats.Resistances.Earth,
            StatId.ResistFire => stats.Resistances.Fire,
            StatId.ResistAir => stats.Resistances.Air,
            StatId.ResistWater => stats.Resistances.Water,
            StatId.ResistNeutral => stats.Resistances.Neutral,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };
}
