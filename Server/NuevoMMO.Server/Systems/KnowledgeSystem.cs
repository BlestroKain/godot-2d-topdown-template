using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public readonly record struct KnowledgeProgressResult(
    KnowledgeEntry Entry,
    bool Discovered,
    bool RankIncreased,
    int PreviousRank);

/// <summary>
/// Reglas de progreso de conocimiento. La curva se inyecta para que diseño/balance pueda cambiarla
/// sin alterar el estado persistido del personaje.
/// </summary>
public sealed class KnowledgeSystem
{
    private readonly long[] rankThresholds;

    public KnowledgeSystem(IEnumerable<long>? rankThresholds = null)
    {
        this.rankThresholds = (rankThresholds ?? new long[] { 0, 100, 500, 1_500, 4_000 })
            .Distinct()
            .Order()
            .ToArray();
        if (this.rankThresholds.Length == 0 || this.rankThresholds[0] != 0 || this.rankThresholds.Any(value => value < 0))
            throw new ArgumentException("La curva de conocimiento debe empezar en 0 y contener valores no negativos.", nameof(rankThresholds));
    }

    public int MaxRank => rankThresholds.Length;

    public KnowledgeProgressResult Observe(
        Knowledge knowledge,
        KnowledgeKind kind,
        DefinitionId definitionId,
        long experience,
        long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        if (definitionId.IsEmpty) throw new ArgumentException("DefinitionId vacío.", nameof(definitionId));
        if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));

        var discovered = !knowledge.TryGet(kind, definitionId, out var entry) || entry is null;
        entry ??= knowledge.Discover(kind, definitionId, nowMilliseconds);
        var previousRank = entry.Rank;
        var totalExperience = checked(entry.Experience + experience);
        var rank = RankFor(totalExperience);
        entry.AddProgress(experience, Math.Max(previousRank, rank), nowMilliseconds, countObservation: !discovered);
        return new(entry, discovered, entry.Rank > previousRank, previousRank);
    }

    public bool MeetsRank(Knowledge knowledge, KnowledgeKind kind, DefinitionId definitionId, int requiredRank)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        if (requiredRank < 1 || requiredRank > MaxRank) throw new ArgumentOutOfRangeException(nameof(requiredRank));
        return knowledge.TryGet(kind, definitionId, out var entry) && entry is not null && entry.Rank >= requiredRank;
    }

    public int RankFor(long experience)
    {
        if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience));
        var rank = 1;
        for (var index = 1; index < rankThresholds.Length; index++)
        {
            if (experience < rankThresholds[index]) break;
            rank = index + 1;
        }
        return rank;
    }
}
