namespace NuevoMMO.Core;

/// <summary>
/// Adaptador compacto para contenido que escala con un atributo primario concreto.
/// Scaling=1 equivale a usar el 100% de la característica en la etapa estilo Dofus.
/// DamageType y ScalingAttribute siguen siendo conceptos independientes.
/// </summary>
public sealed record AttributeDamageFormula(
    Element DamageType,
    PrimaryAttributeId ScalingAttribute,
    float BaseDamage,
    float Scaling,
    float CriticalMultiplier = 1f,
    float Power = 0f,
    float FlatDamage = 0f);

public sealed record AttributeDamageResult(
    Element DamageType,
    PrimaryAttributeId ScalingAttribute,
    float RawDamage,
    float ResistancePercent,
    float FinalDamage,
    bool Critical,
    DamageBreakdown? Breakdown = null);

/// <summary>
/// Compatibilidad y tuning canónico de daño. La implementación completa vive en DamagePipeline;
/// esta clase conserva la API usada por fixtures y contenido temprano.
/// </summary>
public static class CanonicalDamageRules
{
    public const float PositivePveResistanceCap = 80f;

    public static float CalculateRaw(AttributeDamageFormula formula, PrimaryStats attributes)
    {
        var breakdown = ResolvePipeline(formula, attributes, 0f, usePveResistanceCap: false);
        return breakdown.AfterCritical;
    }

    public static float ApplyPveResistance(float rawDamage, float resistancePercent)
    {
        if (!float.IsFinite(rawDamage) || rawDamage < 0)
            throw new ArgumentOutOfRangeException(nameof(rawDamage));
        if (!float.IsFinite(resistancePercent))
            throw new ArgumentOutOfRangeException(nameof(resistancePercent));

        var resistance = Math.Min(resistancePercent, PositivePveResistanceCap);
        var multiplier = Math.Max(0d, (100d - resistance) / 100d);
        return DamagePipeline.FloorNonNegative((double)rawDamage * multiplier);
    }

    public static AttributeDamageResult Resolve(
        AttributeDamageFormula formula,
        PrimaryStats attackerAttributes,
        float targetResistance)
    {
        var breakdown = ResolvePipeline(formula, attackerAttributes, targetResistance, usePveResistanceCap: true);
        return new AttributeDamageResult(
            formula.DamageType,
            formula.ScalingAttribute,
            breakdown.AfterCritical,
            breakdown.EffectiveResistancePercent,
            breakdown.FinalDamage,
            breakdown.Critical,
            breakdown);
    }

    public static DamageBreakdown ResolvePipeline(
        AttributeDamageFormula formula,
        PrimaryStats attackerAttributes,
        float targetResistance,
        bool usePveResistanceCap = true,
        float hardDefense = 0f,
        float softDefense = 0f,
        float flatReduction = 0f,
        float finalMultiplier = 1f)
    {
        ArgumentNullException.ThrowIfNull(formula);
        ArgumentNullException.ThrowIfNull(attackerAttributes);
        if (!Enum.IsDefined(formula.DamageType)) throw new ArgumentOutOfRangeException(nameof(formula.DamageType));
        if (!Enum.IsDefined(formula.ScalingAttribute)) throw new ArgumentOutOfRangeException(nameof(formula.ScalingAttribute));
        if (!float.IsFinite(formula.BaseDamage) || formula.BaseDamage < 0)
            throw new ArgumentOutOfRangeException(nameof(formula.BaseDamage));
        if (!float.IsFinite(formula.Scaling) || formula.Scaling < 0)
            throw new ArgumentOutOfRangeException(nameof(formula.Scaling));
        if (!float.IsFinite(formula.CriticalMultiplier) || formula.CriticalMultiplier < 1f)
            throw new ArgumentOutOfRangeException(nameof(formula.CriticalMultiplier));
        if (!float.IsFinite(formula.Power)) throw new ArgumentOutOfRangeException(nameof(formula.Power));
        if (!float.IsFinite(formula.FlatDamage)) throw new ArgumentOutOfRangeException(nameof(formula.FlatDamage));
        if (!float.IsFinite(targetResistance)) throw new ArgumentOutOfRangeException(nameof(targetResistance));

        var attribute = attackerAttributes.Get(formula.ScalingAttribute);
        if (attribute < 0) throw new ArgumentException("El atributo de escalado no puede ser negativo.", nameof(attackerAttributes));

        return DamagePipeline.Resolve(new DamageCalculationInput(
            formula.DamageType,
            formula.BaseDamage,
            attribute,
            formula.Scaling,
            formula.Power,
            formula.FlatDamage,
            formula.CriticalMultiplier,
            hardDefense,
            softDefense,
            flatReduction,
            targetResistance,
            usePveResistanceCap,
            finalMultiplier));
    }
}
