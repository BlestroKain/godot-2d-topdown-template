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
    bool Killed,
    DamageBreakdown? Breakdown = null);

public sealed record HealingResult(
    EntityId? Source,
    EntityId Target,
    int Requested,
    int Applied);

/// <summary>
/// Núcleo autoritativo de combate. Toda mitigación de daño termina en DamagePipeline.
/// Targeting, alcance, LoS y relaciones son responsabilidades de los sistemas de intención/técnica/IA.
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
    /// Ataque data-driven donde DamageType y ScalingAttribute son independientes.
    /// Usa la fórmula canónica completa y aplica exactamente el FinalDamage calculado.
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

        var breakdown = CanonicalDamageRules.ResolvePipeline(
            formula,
            attacker.Stats.Primary,
            Resistance(target, formula.DamageType),
            usePveResistanceCap: true);
        return ApplyResolvedDamage(attacker, target, breakdown, formula.CriticalMultiplier > 1f, nowMilliseconds);
    }

    /// <summary>
    /// Aplica una cantidad ya resuelta ofensivamente (por ejemplo payload de técnica o mob),
    /// pero obliga a pasar por las etapas defensivas/resistencia del pipeline canónico.
    /// </summary>
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
        if (!Enum.IsDefined(element)) throw new ArgumentOutOfRangeException(nameof(element));
        if (attacker is not null && attacker.MapInstanceId != target.MapInstanceId)
            throw new InvalidOperationException("Atacante y objetivo no están en la misma instancia.");

        var resistance = Resistance(target, element);
        var breakdown = DamagePipeline.Resolve(new DamageCalculationInput(
            element,
            rawDamage,
            Characteristic: 0,
            ResistancePercent: resistance,
            UsePositivePveResistanceCap: usePveResistanceCap));
        return ApplyResolvedDamage(attacker, target, breakdown, critical, nowMilliseconds);
    }

    /// <summary>
    /// Punto único que modifica HP, amenaza, estado de combate y telemetría después de resolver la fórmula.
    /// </summary>
    public DamageResult ApplyResolvedDamage(
        LivingEntity? attacker,
        LivingEntity target,
        DamageBreakdown breakdown,
        bool? critical = null,
        long? nowMilliseconds = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(breakdown);
        if (attacker is not null && attacker.MapInstanceId != target.MapInstanceId)
            throw new InvalidOperationException("Atacante y objetivo no están en la misma instancia.");

        var isCritical = critical ?? breakdown.Critical;
        if (!target.IsAlive)
            return new DamageResult(
                attacker?.Id,
                target.Id,
                breakdown.Element,
                breakdown.AfterCritical,
                breakdown.EffectiveResistancePercent,
                isCritical,
                0,
                false,
                breakdown);

        if (!float.IsFinite(breakdown.FinalDamage) || breakdown.FinalDamage < 0 || breakdown.FinalDamage > int.MaxValue)
            throw new OverflowException("El daño final excede el rango soportado.");

        var requested = (int)breakdown.FinalDamage;
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

        var result = new DamageResult(
            attacker?.Id,
            target.Id,
            breakdown.Element,
            breakdown.AfterCritical,
            breakdown.EffectiveResistancePercent,
            isCritical,
            applied,
            killed,
            breakdown);
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
    /// Escalado aditivo legado para perfiles de criatura y curaciones existentes.
    /// El daño de jugador/técnica debe migrar a DamagePipeline en vez de ampliar esta función.
    /// Los valores de Scaling se interpretan como porcentaje del stat: 100 = 100% del stat.
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

    /// <summary>
    /// Convierte el diccionario de Scaling de una acción en una característica efectiva para la
    /// etapa estilo Dofus. 100 = aporta el 100% del stat, 50 = aporta la mitad, etc.
    /// </summary>
    public static float CalculateEffectiveCharacteristic(
        IReadOnlyDictionary<StatId, float> scaling,
        StatBlock stats)
    {
        ArgumentNullException.ThrowIfNull(scaling);
        ArgumentNullException.ThrowIfNull(stats);
        var result = 0f;
        foreach (var pair in scaling)
        {
            if (!float.IsFinite(pair.Value) || pair.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(scaling), "El peso de escalado debe ser finito y no negativo.");
            result += ReadStat(stats, pair.Key) * (pair.Value / 100f);
        }
        if (!float.IsFinite(result) || result < 0)
            throw new OverflowException("La característica efectiva produjo un valor inválido.");
        return result;
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
