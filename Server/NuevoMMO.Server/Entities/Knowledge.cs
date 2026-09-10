using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public enum KnowledgeKind : byte
{
    General = 0,
    Technique = 1,
    Recipe = 2,
    Resource = 3,
    Creature = 4,
    Item = 5,
    Location = 6,
    Profession = 7
}

public sealed class KnowledgeEntry
{
    internal KnowledgeEntry(KnowledgeKind kind, DefinitionId definitionId, long discoveredAtMilliseconds)
    {
        if (definitionId.IsEmpty) throw new ArgumentException("DefinitionId vacío.", nameof(definitionId));
        if (discoveredAtMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(discoveredAtMilliseconds));
        Kind = kind;
        DefinitionId = definitionId;
        DiscoveredAtMilliseconds = discoveredAtMilliseconds;
        LastProgressAtMilliseconds = discoveredAtMilliseconds;
    }

    public KnowledgeKind Kind { get; }
    public DefinitionId DefinitionId { get; }
    public long Experience { get; private set; }
    public int Rank { get; private set; } = 1;
    public int Observations { get; private set; } = 1;
    public long DiscoveredAtMilliseconds { get; }
    public long LastProgressAtMilliseconds { get; private set; }

    internal void AddProgress(long experience, int rank, long nowMilliseconds, bool countObservation = true)
    {
        if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience));
        if (rank < Rank) throw new ArgumentOutOfRangeException(nameof(rank));
        if (nowMilliseconds < LastProgressAtMilliseconds) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        Experience = checked(Experience + experience);
        Rank = rank;
        if (countObservation) Observations = checked(Observations + 1);
        LastProgressAtMilliseconds = nowMilliseconds;
    }

    internal void SetRank(int rank, long nowMilliseconds)
    {
        if (rank < 1) throw new ArgumentOutOfRangeException(nameof(rank));
        if (nowMilliseconds < LastProgressAtMilliseconds) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        Rank = rank;
        LastProgressAtMilliseconds = nowMilliseconds;
    }
}

/// <summary>
/// Conocimiento permanente del personaje. No decide curvas ni cuánto aprende cada acción;
/// esas reglas pertenecen a KnowledgeSystem/ProfessionSystem. Conserva KnownTechniques por
/// compatibilidad mientras el resto del servidor migra al registro tipado.
/// </summary>
public sealed class Knowledge
{
    private readonly Dictionary<(KnowledgeKind Kind, DefinitionId DefinitionId), KnowledgeEntry> entries = [];

    public HashSet<DefinitionId> KnownTechniques { get; } = [];
    public IReadOnlyCollection<KnowledgeEntry> Entries => entries.Values;

    public bool Knows(KnowledgeKind kind, DefinitionId definitionId)
        => !definitionId.IsEmpty && entries.ContainsKey((kind, definitionId));

    public KnowledgeEntry Get(KnowledgeKind kind, DefinitionId definitionId)
        => entries.TryGetValue((kind, definitionId), out var entry)
            ? entry
            : throw new KeyNotFoundException($"No existe conocimiento {kind}:{definitionId.Value}.");

    public bool TryGet(KnowledgeKind kind, DefinitionId definitionId, out KnowledgeEntry? entry)
        => entries.TryGetValue((kind, definitionId), out entry);

    public KnowledgeEntry Discover(KnowledgeKind kind, DefinitionId definitionId, long nowMilliseconds)
    {
        if (definitionId.IsEmpty) throw new ArgumentException("DefinitionId vacío.", nameof(definitionId));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));

        if (entries.TryGetValue((kind, definitionId), out var existing)) return existing;
        var entry = new KnowledgeEntry(kind, definitionId, nowMilliseconds);
        entries.Add((kind, definitionId), entry);
        if (kind == KnowledgeKind.Technique) KnownTechniques.Add(definitionId);
        return entry;
    }

    public bool Forget(KnowledgeKind kind, DefinitionId definitionId)
    {
        var removed = entries.Remove((kind, definitionId));
        if (removed && kind == KnowledgeKind.Technique) KnownTechniques.Remove(definitionId);
        return removed;
    }

    public void Clear()
    {
        entries.Clear();
        KnownTechniques.Clear();
    }
}
