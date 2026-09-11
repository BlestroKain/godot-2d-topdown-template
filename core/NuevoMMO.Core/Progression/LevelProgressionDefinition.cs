namespace NuevoMMO.Core;

/// <summary>
/// Curva de XP V0.1. ExperienceToNextLevel(L) = round(100 * L^1.65) hasta el cap de referencia.
/// </summary>
public sealed class LevelProgressionDefinition
{
    public const int DefaultLevelCap = 100;
    public const double ExperienceExponent = 1.65;
    public const double ExperienceBase = 100;

    public int ReferenceLevelCap { get; }
    public int LevelCap => ReferenceLevelCap;

    public LevelProgressionDefinition(int levelCap = DefaultLevelCap)
    {
        if (levelCap < 2) throw new ArgumentOutOfRangeException(nameof(levelCap));
        ReferenceLevelCap = levelCap;
    }

    public static LevelProgressionDefinition CreateV01Seed() => new(DefaultLevelCap);

    public void Validate()
    {
        if (ReferenceLevelCap < 2) throw new InvalidOperationException("LevelCap debe ser al menos 2.");
        _ = ExperienceToNextLevel(1);
    }

    public int ExperienceToNextLevel(int level)
    {
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        if (level >= ReferenceLevelCap) return 0;
        var value = Math.Round(ExperienceBase * Math.Pow(level, ExperienceExponent), MidpointRounding.AwayFromZero);
        if (value < 1 || value > int.MaxValue)
            throw new OverflowException($"XP de nivel {level} fuera de rango.");
        return (int)value;
    }
}
