namespace NuevoMMO.Core;

/// <summary>
/// Entrada determinista al cálculo canónico de daño.
/// El elemento y el atributo que escala siguen siendo conceptos independientes.
/// RNG, selección de objetivo y aplicación de HP pertenecen al runtime autoritativo, no a esta fórmula.
/// </summary>
public sealed record DamageCalculationInput(
    Element Element,
    float BaseDamage,
    float Characteristic,
    float CharacteristicScale = 1f,
    float Power = 0f,
    float FlatDamage = 0f,
    float CriticalMultiplier = 1f,
    float HardDefense = 0f,
    float SoftDefense = 0f,
    float FlatReduction = 0f,
    float ResistancePercent = 0f,
    bool UsePositivePveResistanceCap = false,
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
    float CriticalMultiplier,
    float AfterCritical,
    float HardDefense,
    float HardDefenseMultiplier,
    float AfterHardDefense,
    float SoftDefense,
    float AfterSoftDefense,
    float FlatReduction,
    float AfterFlatReduction,
    float ResistancePercent,
    float EffectiveResistancePercent,
    float AfterResistance,
    float FinalMultiplier,
    float FinalDamage)
{
    public bool Critical => CriticalMultiplier > 1f;
}

/// <summary>
/// Fórmula híbrida del proyecto:
/// 1) columna ofensiva estilo Dofus: daño base × (100 + característica efectiva + Power) / 100, luego daño plano;
/// 2) defensa dura/blanda inspirada en Ragnarok Renewal;
/// 3) reducciones y resistencias elementales/neutral propias del proyecto;
/// 4) multiplicador final reservado para modificadores como "Daño a X".
///
/// No deriva HardDefense/SoftDefense desde VIT aquí: esa conversión pertenece al sistema de stats y se
/// fijará por balance. El pipeline solo consume valores ya resueltos.
/// </summary>
public static class DamagePipeline
{
    /// <summary>
    /// Constante de la curva de Hard DEF inspirada en Ragnarok Renewal.
    /// Está centralizada para poder tunearla sin repartir números mágicos por el runtime.
    /// </summary>
    public const float HardDefenseCurve = 4000f;

    public static DamageBreakdown Resolve(DamageCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input);

        var effectiveOffense = (double)input.Characteristic * input.CharacteristicScale + input.Power;
        var offenseFactor = Math.Max(0d, (100d + effectiveOffense) / 100d);
        var afterCharacteristic = FloorNonNegative((double)input.BaseDamage * offenseFactor);

        var afterFlat = FloorNonNegative((double)afterCharacteristic + input.FlatDamage);
        var afterCritical = FloorNonNegative((double)afterFlat * input.CriticalMultiplier);

        var hardMultiplier = HardDefenseMultiplier(input.HardDefense);
        var afterHard = FloorNonNegative((double)afterCritical * hardMultiplier);
        var afterSoft = FloorNonNegative((double)afterHard - input.SoftDefense);
        var afterFlatReduction = FloorNonNegative((double)afterSoft - input.FlatReduction);

        var effectiveResistance = input.UsePositivePveResistanceCap
            ? Math.Min(input.ResistancePercent, CanonicalDamageRules.PositivePveResistanceCap)
            : input.ResistancePercent;
        var resistanceMultiplier = Math.Max(0d, (100d - effectiveResistance) / 100d);
        var afterResistance = FloorNonNegative((double)afterFlatReduction * resistanceMultiplier);
        var final = FloorNonNegative((double)afterResistance * input.FinalMultiplier);

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
            input.CriticalMultiplier,
            afterCritical,
            input.HardDefense,
            hardMultiplier,
            afterHard,
            input.SoftDefense,
            afterSoft,
            input.FlatReduction,
            afterFlatReduction,
            input.ResistancePercent,
            effectiveResistance,
            afterResistance,
            input.FinalMultiplier,
            final);
    }

    public static float HardDefenseMultiplier(float hardDefense)
    {
        if (!float.IsFinite(hardDefense) || hardDefense < 0f)
            throw new ArgumentOutOfRangeException(nameof(hardDefense));
        var defense = (double)hardDefense;
        var denominator = HardDefenseCurve + 10d * defense;
        var multiplier = (HardDefenseCurve + defense) / denominator;
        if (!double.IsFinite(multiplier) || multiplier < 0d || multiplier > 1d)
            throw new OverflowException("La defensa dura produjo un multiplicador inválido.");
        return (float)multiplier;
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
        if (!float.IsFinite(input.CriticalMultiplier) || input.CriticalMultiplier < 1f)
            throw new ArgumentOutOfRangeException(nameof(input.CriticalMultiplier));
        RequireFiniteNonNegative(input.HardDefense, nameof(input.HardDefense));
        RequireFiniteNonNegative(input.SoftDefense, nameof(input.SoftDefense));
        RequireFiniteNonNegative(input.FlatReduction, nameof(input.FlatReduction));
        RequireFinite(input.ResistancePercent, nameof(input.ResistancePercent));
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
