namespace NuevoMMO.Core;

/// <summary>
/// Reglas V0.1 para pruebas de progresión. La arquitectura es estable; los
/// números de tuning pueden cambiar después de medirlos en juego.
/// </summary>
public static class ProgressionRules
{
    public const int ReferenceLevelCap = 100;
    public const int AttributePointsPerLevel = 3;
    public const int BaseNaturalAttribute = 10;
    public const int MaximumNaturalAttribute = 300;

    public static PlayerProgressionState CreateInitial() => new(
        Level: 1,
        Experience: 0,
        AvailableAttributePoints: 0,
        NaturalAttributes: new NaturalPrimaryStats(
            BaseNaturalAttribute,
            BaseNaturalAttribute,
            BaseNaturalAttribute,
            BaseNaturalAttribute,
            BaseNaturalAttribute));

    public static int EarnedAttributePoints(int level)
    {
        if (level < 1 || level > ReferenceLevelCap)
            throw new ArgumentOutOfRangeException(nameof(level));
        return checked((level - 1) * AttributePointsPerLevel);
    }

    public static int CostForNextPoint(int currentNaturalValue)
    {
        if (currentNaturalValue < BaseNaturalAttribute || currentNaturalValue >= MaximumNaturalAttribute)
            throw new ArgumentOutOfRangeException(nameof(currentNaturalValue));

        var target = currentNaturalValue + 1;
        return target switch
        {
            <= 101 => 1,
            <= 200 => 2,
            <= 300 => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(currentNaturalValue))
        };
    }

    public static int SpentAttributePoints(NaturalPrimaryStats attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        return CostToReach(attributes.Strength)
             + CostToReach(attributes.Intelligence)
             + CostToReach(attributes.Agility)
             + CostToReach(attributes.Spirit)
             + CostToReach(attributes.Vitality);
    }

    public static void Validate(PlayerProgressionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Level < 1 || state.Level > ReferenceLevelCap)
            throw new ArgumentOutOfRangeException(nameof(state), "Nivel fuera del cap V0.1.");
        if (state.Experience < 0)
            throw new ArgumentException("La experiencia no puede ser negativa.", nameof(state));
        if (state.AvailableAttributePoints < 0)
            throw new ArgumentException("Los puntos disponibles no pueden ser negativos.", nameof(state));

        var spent = SpentAttributePoints(state.NaturalAttributes);
        if (spent + state.AvailableAttributePoints != EarnedAttributePoints(state.Level))
            throw new ArgumentException("Distribución de atributos incompatible con el nivel.", nameof(state));
    }

    public static PlayerProgressionState Allocate(
        PlayerProgressionState state,
        PrimaryAttributeId attribute,
        int increments = 1)
    {
        Validate(state);
        if (!Enum.IsDefined(attribute)) throw new ArgumentOutOfRangeException(nameof(attribute));
        if (increments <= 0) throw new ArgumentOutOfRangeException(nameof(increments));

        var natural = state.NaturalAttributes;
        var remaining = state.AvailableAttributePoints;
        for (var index = 0; index < increments; index++)
        {
            var current = natural.Get(attribute);
            var cost = CostForNextPoint(current);
            if (remaining < cost)
                throw new InvalidOperationException("Puntos de atributo insuficientes.");
            natural = natural.With(attribute, current + 1);
            remaining -= cost;
        }

        var result = state with
        {
            NaturalAttributes = natural,
            AvailableAttributePoints = remaining
        };
        Validate(result);
        return result;
    }

    public static PlayerProgressionState GainExperience(
        PlayerProgressionState state,
        long amount,
        LevelProgressionDefinition progression)
    {
        Validate(state);
        ArgumentNullException.ThrowIfNull(progression);
        progression.Validate();
        if (progression.ReferenceLevelCap != ReferenceLevelCap)
            throw new ArgumentException("La tabla no corresponde al cap V0.1.", nameof(progression));
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

        var level = state.Level;
        var experience = checked(state.Experience + amount);
        var available = state.AvailableAttributePoints;

        while (level < progression.ReferenceLevelCap)
        {
            var required = progression.ExperienceToNextLevel(level);
            if (experience < required) break;
            experience -= required;
            level++;
            available = checked(available + AttributePointsPerLevel);
        }

        var result = state with
        {
            Level = level,
            Experience = experience,
            AvailableAttributePoints = available
        };
        Validate(result);
        return result;
    }

    private static int CostToReach(int value)
    {
        if (value < BaseNaturalAttribute || value > MaximumNaturalAttribute)
            throw new ArgumentOutOfRangeException(nameof(value));
        var total = 0;
        for (var current = BaseNaturalAttribute; current < value; current++)
            total = checked(total + CostForNextPoint(current));
        return total;
    }
}
