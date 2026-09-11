namespace NuevoMMO.Core;

/// <summary>
/// Resultado derivado de nivel + atributos efectivos. No debe persistirse como
/// verdad paralela del personaje.
/// </summary>
public sealed record EffectiveStats(
    int MaxHealth,
    int MaxMana,
    float Defense,
    float ManaRegenPerSecond,
    float OutOfCombatManaRegenPerSecond);

/// <summary>
/// Fórmulas de prueba V0.1. Se centralizan para poder retocar tuning sin tocar
/// persistencia, entidades ni protocolo.
/// </summary>
public static class CanonicalStatCalculator
{
    public static EffectiveStats Calculate(int level, PrimaryStats attributes, float externalDefense = 0)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        if (level < 1 || level > ProgressionRules.ReferenceLevelCap)
            throw new ArgumentOutOfRangeException(nameof(level));
        if (attributes.Strength < 0 || attributes.Intelligence < 0 || attributes.Agility < 0 ||
            attributes.Spirit < 0 || attributes.Vitality < 0)
            throw new ArgumentException("Los atributos efectivos no pueden ser negativos.", nameof(attributes));
        if (!float.IsFinite(externalDefense))
            throw new ArgumentOutOfRangeException(nameof(externalDefense));

        var maxHealth = checked(300 + level * 10 + attributes.Vitality * 12);
        var maxMana = checked(100 + level * 2 + attributes.Spirit * 6);
        var defense = attributes.Vitality * 0.25f + externalDefense;
        var manaRegen = 1f + attributes.Spirit * 0.02f;

        return new EffectiveStats(
            maxHealth,
            maxMana,
            defense,
            manaRegen,
            manaRegen * 3f);
    }
}
