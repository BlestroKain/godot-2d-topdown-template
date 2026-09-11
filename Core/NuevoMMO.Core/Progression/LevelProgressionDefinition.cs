namespace NuevoMMO.Core;

/// <summary>
/// Tabla editable de experiencia. La fórmula usada por el tuning V0.1 solo
/// genera una semilla inicial; el servidor consume la tabla, no una ley hardcodeada.
/// </summary>
public sealed record LevelProgressionDefinition(
    int ReferenceLevelCap,
    Dictionary<int, long> RequiredExperienceByLevel)
{
    public void Validate()
    {
        if (ReferenceLevelCap < 2)
            throw new ArgumentOutOfRangeException(nameof(ReferenceLevelCap));
        ArgumentNullException.ThrowIfNull(RequiredExperienceByLevel);

        for (var level = 1; level < ReferenceLevelCap; level++)
        {
            if (!RequiredExperienceByLevel.TryGetValue(level, out var value) || value <= 0)
                throw new ArgumentException($"Falta experiencia válida para nivel {level}.", nameof(RequiredExperienceByLevel));
        }
    }

    public long ExperienceToNextLevel(int level)
    {
        Validate();
        if (level < 1 || level >= ReferenceLevelCap)
            throw new ArgumentOutOfRangeException(nameof(level));
        return RequiredExperienceByLevel[level];
    }

    public static LevelProgressionDefinition CreateV01Seed()
    {
        var values = new Dictionary<int, long>();
        for (var level = 1; level < ProgressionRules.ReferenceLevelCap; level++)
        {
            values[level] = checked((long)Math.Round(
                100d * Math.Pow(level, 1.65d),
                MidpointRounding.AwayFromZero));
        }

        var result = new LevelProgressionDefinition(ProgressionRules.ReferenceLevelCap, values);
        result.Validate();
        return result;
    }
}
