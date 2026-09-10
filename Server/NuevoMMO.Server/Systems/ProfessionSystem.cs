using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public sealed record ProfessionProgressChange(
    DefinitionId ProfessionId,
    long PreviousExperience,
    long Experience,
    int PreviousLevel,
    int Level,
    bool LeveledUp);

/// <summary>
/// Reglas autoritativas de progresión profesional. La profesión avanza separada del nivel
/// del personaje y usa la curva/dimensiones declaradas por ProfessionDefinition.
/// </summary>
public sealed class ProfessionSystem
{
    private readonly DefinitionRegistry definitions;

    public ProfessionSystem(DefinitionRegistry definitions)
        => this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));

    public ProfessionProgress Learn(Player player, DefinitionId professionId)
    {
        ArgumentNullException.ThrowIfNull(player);
        var definition = RequireDefinition(professionId);
        if (!definition.Enabled) throw new InvalidOperationException("La profesión está deshabilitada.");
        return player.Professions.Learn(professionId);
    }

    public ProfessionProgressChange GrantExperience(Player player, DefinitionId professionId, long amount)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var definition = RequireDefinition(professionId);
        var progress = player.Professions.Get(professionId);
        var previousExperience = progress.Experience;
        var previousLevel = progress.Level;
        var experience = checked(previousExperience + amount);
        var level = CalculateLevel(definition, experience);
        progress.SetProgress(experience, level);
        return new ProfessionProgressChange(professionId, previousExperience, experience, previousLevel, level, level > previousLevel);
    }

    public float GrantMastery(Player player, DefinitionId professionId, string dimension, float amount)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (string.IsNullOrWhiteSpace(dimension)) throw new ArgumentException("Dimensión requerida.", nameof(dimension));
        if (!float.IsFinite(amount) || amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        var definition = RequireDefinition(professionId);
        var key = ResolveDimension(definition, dimension);
        var progress = player.Professions.Get(professionId);
        progress.AddMastery(key, amount);
        return progress.Mastery[key];
    }

    public bool SelectSpecialization(Player player, DefinitionId professionId, string specializationKey, bool replaceExisting = true)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (string.IsNullOrWhiteSpace(specializationKey))
            throw new ArgumentException("Especialización requerida.", nameof(specializationKey));

        var definition = RequireDefinition(professionId);
        var key = definition.Specializations.Keys.FirstOrDefault(value =>
            string.Equals(value, specializationKey.Trim(), StringComparison.OrdinalIgnoreCase));
        if (key is null) throw new KeyNotFoundException($"La especialización '{specializationKey}' no existe en {definition.Name}.");

        var progress = player.Professions.Get(professionId);
        if (replaceExisting) progress.ReplaceSpecializations([key]);
        else progress.AddSpecialization(key);
        return true;
    }

    public IReadOnlySet<DefinitionId> GrantedTechniques(Player player, DefinitionId professionId)
    {
        ArgumentNullException.ThrowIfNull(player);
        var definition = RequireDefinition(professionId);
        var progress = player.Professions.Get(professionId);
        var result = definition.TechniqueIds.ToHashSet();

        foreach (var specializationKey in progress.Specializations)
        {
            if (!definition.Specializations.TryGetValue(specializationKey, out var specialization)) continue;
            foreach (var techniqueId in specialization.TechniqueIds) result.Add(techniqueId);
        }
        return result;
    }

    public int CalculateLevel(ProfessionDefinition definition, long experience)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience));
        var curve = definition.Mastery.ExperienceRequirements;
        if (curve.Count == 0) return 1;

        var level = 1;
        foreach (var requirement in curve.OrderBy(static pair => pair.Key))
        {
            if (experience < requirement.Value) break;
            level = Math.Max(level, requirement.Key);
        }
        return level;
    }

    private ProfessionDefinition RequireDefinition(DefinitionId professionId)
    {
        if (professionId.IsEmpty) throw new ArgumentException("ProfessionId vacío.", nameof(professionId));
        return definitions.Get<ProfessionDefinition>(professionId);
    }

    private static string ResolveDimension(ProfessionDefinition definition, string dimension)
    {
        var requested = dimension.Trim();
        var resolved = definition.Mastery.Dimensions.FirstOrDefault(value =>
            string.Equals(value, requested, StringComparison.OrdinalIgnoreCase));
        if (resolved is null)
            throw new KeyNotFoundException($"La dimensión de maestría '{requested}' no existe en {definition.Name}.");
        return resolved;
    }
}
