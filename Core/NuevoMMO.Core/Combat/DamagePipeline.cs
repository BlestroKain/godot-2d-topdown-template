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

        var effectiveOffense = input.Characteristic * input.CharacteristicScale + input.Power;
        var offenseFactor = MathF.Max(0f, 1f + effectiveOffense / 100f);
        var afterCharacteristic = FloorNonNegative(input.BaseDamage * offenseFactor);

        var afterFlat = FloorNonNegative(afterCharacteristic + input.FlatDamage);
        var afterCritical = FloorNonNegative(afterFlat * input.CriticalMultiplier);

        var hardMultiplier = HardDefenseMultiplier(input.HardDefense);
        var afterHard = FloorNonNegative(afterCritical * hardMultiplier);
        var afterSoft = FloorNonNegative(afterHard - input.SoftDefense);
        var afterFlatReduction = FloorNonNegative(afterSoft - input.FlatReduction);

        var effectiveResistance = input.UsePositivePveResistanceCap
            ? Math.Min(input.ResistancePercent, CanonicalDamageRules.PositivePveResistanceCap)
            : input.ResistancePercent;
        var resistanceMultiplier = MathF.Max(0f, 1f - effectiveResistance / 100f);
        var afterResistance = FloorNonNegative(afterFlatReduction * resistanceMultiplier);
        var final = FloorNonNegative(afterResistance * input.FinalMultiplier);

        return new DamageBreakdown(
            input.Element,
            input.BaseDamage,
            input.Characteristic,
            input.CharacteristicScale,
            input.Power,
            effectiveOffense,
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
        var denominator = HardDefenseCurve + 10f * hardDefense;
        var multiplier = (HardDefenseCurve + hardDefense) / denominator;
        if (!float.IsFinite(multiplier) || multiplier < 0f || multiplier > 1f)
            throw new OverflowException("La defensa dura produjo un multiplicador inválido.");
        return multiplier;
    }

    private static float FloorNonNegative(float value)
    {
        if (!float.IsFinite(value)) throw new OverflowException("El daño produjo un valor no finito.");
        return MathF.Floor(MathF.Max(0f, value));
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
