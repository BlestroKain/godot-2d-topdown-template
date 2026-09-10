using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class ProfessionProgress
{
    private readonly Dictionary<string, float> mastery = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> specializations = new(StringComparer.OrdinalIgnoreCase);

    public ProfessionProgress(DefinitionId professionId, long experience = 0, int level = 1)
    {
        if (professionId.IsEmpty) throw new ArgumentException("ProfessionId vacío.", nameof(professionId));
        if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience));
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        ProfessionId = professionId;
        Experience = experience;
        Level = level;
    }

    public DefinitionId ProfessionId { get; }
    public long Experience { get; private set; }
    public int Level { get; private set; }
    public IReadOnlyDictionary<string, float> Mastery => mastery;
    public IReadOnlySet<string> Specializations => specializations;

    public void SetProgress(long experience, int level)
    {
        if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience));
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        Experience = experience;
        Level = level;
    }

    public void AddMastery(string dimension, float amount)
    {
        if (string.IsNullOrWhiteSpace(dimension)) throw new ArgumentException("Dimensión requerida.", nameof(dimension));
        if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var key = dimension.Trim();
        mastery[key] = mastery.GetValueOrDefault(key) + amount;
    }

    public void SetMastery(string dimension, float value)
    {
        if (string.IsNullOrWhiteSpace(dimension)) throw new ArgumentException("Dimensión requerida.", nameof(dimension));
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        mastery[dimension.Trim()] = value;
    }

    public bool AddSpecialization(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Especialización requerida.", nameof(key));
        return specializations.Add(key.Trim());
    }

    public bool RemoveSpecialization(string key)
        => !string.IsNullOrWhiteSpace(key) && specializations.Remove(key.Trim());

    public void ReplaceSpecializations(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var incoming = keys.Select(static key => key?.Trim() ?? string.Empty).ToArray();
        if (incoming.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Especialización vacía.", nameof(keys));
        specializations.Clear();
        foreach (var key in incoming) specializations.Add(key);
    }
}

/// <summary>
/// Progresión profesional independiente del nivel del personaje.
/// </summary>
public sealed class ProfessionSet
{
    private readonly Dictionary<DefinitionId, ProfessionProgress> professions = [];

    public IReadOnlyDictionary<DefinitionId, ProfessionProgress> Professions => professions;
    public IReadOnlySet<DefinitionId> ProfessionIds => professions.Keys.ToHashSet();

    public bool Knows(DefinitionId professionId)
        => !professionId.IsEmpty && professions.ContainsKey(professionId);

    public ProfessionProgress Get(DefinitionId professionId)
        => professions.TryGetValue(professionId, out var progress)
            ? progress
            : throw new KeyNotFoundException($"La profesión {professionId} no está aprendida.");

    public bool TryGet(DefinitionId professionId, out ProfessionProgress? progress)
        => professions.TryGetValue(professionId, out progress);

    public ProfessionProgress Learn(DefinitionId professionId)
    {
        if (professionId.IsEmpty) throw new ArgumentException("ProfessionId vacío.", nameof(professionId));
        if (professions.TryGetValue(professionId, out var existing)) return existing;
        var progress = new ProfessionProgress(professionId);
        professions.Add(professionId, progress);
        return progress;
    }

    public void Restore(ProfessionProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (!professions.TryAdd(progress.ProfessionId, progress))
            throw new InvalidOperationException($"La profesión {progress.ProfessionId} ya está cargada.");
    }

    public bool Forget(DefinitionId professionId) => professions.Remove(professionId);
    public void Clear() => professions.Clear();
}
