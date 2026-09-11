namespace NuevoMMO.Core;

/// <summary>
/// Fórmula de daño donde el elemento y el atributo de escalado son conceptos
/// independientes. Neutral, por ejemplo, no implica Strength automáticamente.
/// </summary>
public sealed record AttributeDamageFormula(
    Element DamageType,
    PrimaryAttributeId ScalingAttribute,
    float BaseDamage,
    float Scaling,
    float CriticalMultiplier = 1f);

public sealed record AttributeDamageResult(
    Element DamageType,
    PrimaryAttributeId ScalingAttribute,
    float RawDamage,
    float ResistancePercent,
    float FinalDamage,
    bool Critical);

/// <summary>
/// Tuning de combate V0.1 para pruebas. El cap PvE puede cambiar después de
/// medir builds y criaturas; se mantiene centralizado para no hardcodearlo.
/// </summary>
public static class CanonicalDamageRules
{
    public const float PositivePveResistanceCap = 80f;

    public static float CalculateRaw(AttributeDamageFormula formula, PrimaryStats attributes)
    {
        ArgumentNullException.ThrowIfNull(formula);
        ArgumentNullException.ThrowIfNull(attributes);
        if (!Enum.IsDefined(formula.DamageType)) throw new ArgumentOutOfRangeException(nameof(formula.DamageType));
        if (!Enum.IsDefined(formula.ScalingAttribute)) throw new ArgumentOutOfRangeException(nameof(formula.ScalingAttribute));
        if (!float.IsFinite(formula.BaseDamage) || formula.BaseDamage < 0)
            throw new ArgumentOutOfRangeException(nameof(formula.BaseDamage));
        if (!float.IsFinite(formula.Scaling) || formula.Scaling < 0)
            throw new ArgumentOutOfRangeException(nameof(formula.Scaling));
        if (!float.IsFinite(formula.CriticalMultiplier) || formula.CriticalMultiplier < 1f)
            throw new ArgumentOutOfRangeException(nameof(formula.CriticalMultiplier));

        var attribute = attributes.Get(formula.ScalingAttribute);
        if (attribute < 0) throw new ArgumentException("El atributo de escalado no puede ser negativo.", nameof(attributes));

        var raw = formula.BaseDamage * (1f + attribute / 100f * formula.Scaling);
        raw *= formula.CriticalMultiplier;
        if (!float.IsFinite(raw)) throw new OverflowException("El daño produjo un valor no finito.");
        return raw;
    }

    public static float ApplyPveResistance(float rawDamage, float resistancePercent)
    {
        if (!float.IsFinite(rawDamage) || rawDamage < 0)
            throw new ArgumentOutOfRangeException(nameof(rawDamage));
        if (!float.IsFinite(resistancePercent))
            throw new ArgumentOutOfRangeException(nameof(resistancePercent));

        var resistance = Math.Min(resistancePercent, PositivePveResistanceCap);
        var result = rawDamage * (1f - resistance / 100f);
        if (!float.IsFinite(result) || result < 0)
            throw new OverflowException("La mitigación produjo daño inválido.");
        return result;
    }

    public static AttributeDamageResult Resolve(
        AttributeDamageFormula formula,
        PrimaryStats attackerAttributes,
        float targetResistance)
    {
        var raw = CalculateRaw(formula, attackerAttributes);
        var final = ApplyPveResistance(raw, targetResistance);
        return new AttributeDamageResult(
            formula.DamageType,
            formula.ScalingAttribute,
            raw,
            Math.Min(targetResistance, PositivePveResistanceCap),
            final,
            formula.CriticalMultiplier > 1f);
    }
}
