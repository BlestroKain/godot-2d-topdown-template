namespace NuevoMMO.Core;

/// <summary>
/// Entrada determinista al cálculo canónico de daño.
/// El elemento y el atributo que escala son conceptos independientes.
/// RNG, selección de objetivo y aplicación de HP pertenecen al runtime autoritativo.
/// </summary>
public sealed record DamageCalculationInput(
    Element Element,
    float BaseDamage,
    float Characteristic,
    float CharacteristicScale = 1f,
    float Power = 0f,
    float FlatDamage = 0f,
    float FlatReduction = 0f,
    float ResistancePercent = 0f,
    bool UsePositivePveResistanceCap = false,
    float CriticalMultiplier = 1f,
    float FinalMultiplier = 1f);

/// <summary>
/// Desglose completo del pipeline para tests, telemetría y UI de depuración.
/// Las etapas se truncan hacia abajo deliberadamente para que la aritmética sea estable y reproducible.
/// </summary>
public sealed record DamageBreakdown(
    Element Element,
    float BaseDamage,
    float Characteristic,
    float CharacteristicScale,
    float Power,
    float EffectiveOffense,
    float AfterCharacteristicAndPower,
    float FlatDamage,
    float AfterFlatDamage,
    float FlatReduction,
    float AfterFlatReduction,
    float ResistancePercent,
    float EffectiveResistancePercent,
    float AfterResistance,
    float CriticalMultiplier,
    float AfterCritical,
    float FinalMultiplier,
    float FinalDamage)
{
    public bool Critical => CriticalMultiplier > 1f;
}

/// <summary>
/// Fórmula canónica del proyecto, basada en la columna ofensiva clásica de Dofus:
///
///     Daño ofensivo = DB × (100 + SA + P) / 100 + DF
///
/// donde DB es daño base, SA la característica efectiva, P Potencia y DF daños fijos.
/// Después se aplican, en este orden:
/// 1) reducción fija;
/// 2) resistencia porcentual (la resistencia negativa funciona como vulnerabilidad);
/// 3) crítico y otros multiplicadores finales situacionales.
///
/// No existe una segunda capa Hard DEF/Soft DEF dentro de esta fórmula. Si VIT, equipo o efectos
/// producen defensa, deben resolverla a reducción fija, resistencia o un modificador explícito antes
/// de llegar aquí. Así todas las fuentes de daño comparten una sola matemática legible.
/// </summary>
public static class DamagePipeline
{
    public static DamageBreakdown Resolve(DamageCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input);

        var effectiveOffense = (double)input.Characteristic * input.CharacteristicScale + input.Power;
        var offenseFactor = Math.Max(0d, (100d + effectiveOffense) / 100d);
        var afterCharacteristic = FloorNonNegative((double)input.BaseDamage * offenseFactor);
        var afterFlat = FloorNonNegative((double)afterCharacteristic + input.FlatDamage);
        var afterFlatReduction = FloorNonNegative((double)afterFlat - input.FlatReduction);

        var effectiveResistance = input.UsePositivePveResistanceCap
            ? Math.Min(input.ResistancePercent, CanonicalDamageRules.PositivePveResistanceCap)
            : input.ResistancePercent;
        var resistanceMultiplier = Math.Max(0d, (100d - effectiveResistance) / 100d);
        var afterResistance = FloorNonNegative((double)afterFlatReduction * resistanceMultiplier);

        var afterCritical = FloorNonNegative((double)afterResistance * input.CriticalMultiplier);
        var final = FloorNonNegative((double)afterCritical * input.FinalMultiplier);

        return new DamageBreakdown(
            input.Element,
            input.BaseDamage,
            input.Characteristic,
            input.CharacteristicScale,
            input.Power,
            (float)effectiveOffense,
            afterCharacteristic,
            input.FlatDamage,
            afterFlat,
            input.FlatReduction,
            afterFlatReduction,
            input.ResistancePercent,
            effectiveResistance,
            afterResistance,
            input.CriticalMultiplier,
            afterCritical,
            input.FinalMultiplier,
            final);
    }

    internal static float FloorNonNegative(double value)
    {
        if (!double.IsFinite(value)) throw new OverflowException("El daño produjo un valor no finito.");
        var clamped = Math.Max(0d, value);
        var floored = Math.Floor(clamped);
        if (floored > float.MaxValue) throw new OverflowException("El daño excede el rango soportado.");
        return (float)floored;
    }

    private static void Validate(DamageCalculationInput input)
    {
        if (!Enum.IsDefined(input.Element)) throw new ArgumentOutOfRangeException(nameof(input.Element));
        RequireFiniteNonNegative(input.BaseDamage, nameof(input.BaseDamage));
        RequireFiniteNonNegative(input.Characteristic, nameof(input.Characteristic));
        RequireFiniteNonNegative(input.CharacteristicScale, nameof(input.CharacteristicScale));
        RequireFinite(input.Power, nameof(input.Power));
        RequireFinite(input.FlatDamage, nameof(input.FlatDamage));
        RequireFiniteNonNegative(input.FlatReduction, nameof(input.FlatReduction));
        RequireFinite(input.ResistancePercent, nameof(input.ResistancePercent));
        if (!float.IsFinite(input.CriticalMultiplier) || input.CriticalMultiplier < 1f)
            throw new ArgumentOutOfRangeException(nameof(input.CriticalMultiplier));
        RequireFiniteNonNegative(input.FinalMultiplier, nameof(input.FinalMultiplier));
    }

    private static void RequireFinite(float value, string name)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(name);
    }

    private static void RequireFiniteNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f) throw new ArgumentOutOfRangeException(name);
    }
}
