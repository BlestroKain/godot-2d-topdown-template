namespace NuevoMMO.Core;

/// <summary>
/// Define un dungeon como lugar real compuesto por uno o varios mapas y etapas.
/// No obliga a que sea instanciado: OpenWorld es el modo predeterminado.
/// </summary>
public sealed record DungeonDefinition : GameDefinition
{
    public DungeonDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        DefinitionId mapId,
        DefinitionId[]? mapIds = null,
        DungeonStageDefinition[]? stages = null,
        DungeonInstanceMode instanceMode = DungeonInstanceMode.OpenWorld,
        ConditionGroupDefinition? entryRequirements = null,
        DefinitionId? entryEventId = null,
        DefinitionId? completionEventId = null,
        int resetMilliseconds = 0,
        int minimumPartySize = 1,
        int maximumPartySize = 0,
        Dictionary<string, float>? parameters = null,
        Dictionary<string, string>? metadata = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (mapId.IsEmpty) throw new ArgumentException("MapId vacío.", nameof(mapId));
        if (entryEventId is { } entry && entry.IsEmpty) throw new ArgumentException("EntryEventId vacío.", nameof(entryEventId));
        if (completionEventId is { } completion && completion.IsEmpty)
            throw new ArgumentException("CompletionEventId vacío.", nameof(completionEventId));
        if (resetMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(resetMilliseconds));
        if (minimumPartySize < 1) throw new ArgumentOutOfRangeException(nameof(minimumPartySize));
        if (maximumPartySize < 0) throw new ArgumentOutOfRangeException(nameof(maximumPartySize));
        if (maximumPartySize > 0 && maximumPartySize < minimumPartySize)
            throw new ArgumentOutOfRangeException(nameof(maximumPartySize));

        var maps = DefinitionModelGuards.CopyIds(mapIds, nameof(mapIds)).ToList();
        if (!maps.Contains(mapId)) maps.Insert(0, mapId);

        var normalizedStages = stages?.ToArray() ?? [];
        if (normalizedStages.Select(static stage => stage.Id).Distinct().Count() != normalizedStages.Length)
            throw new ArgumentException("Stages contiene IDs duplicados.", nameof(stages));

        MapId = mapId;
        MapIds = maps.ToArray();
        Stages = normalizedStages;
        InstanceMode = instanceMode;
        EntryRequirements = entryRequirements ?? ConditionGroupDefinition.Empty;
        EntryEventId = entryEventId;
        CompletionEventId = completionEventId;
        ResetMilliseconds = resetMilliseconds;
        MinimumPartySize = minimumPartySize;
        MaximumPartySize = maximumPartySize;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
        Metadata = DefinitionCollectionGuards.CopyText(metadata, nameof(metadata));
    }

    /// <summary>Mapa principal/entrada para compatibilidad y resolución rápida.</summary>
    public DefinitionId MapId { get; }
    public DefinitionId[] MapIds { get; }
    public DungeonStageDefinition[] Stages { get; }
    public DungeonInstanceMode InstanceMode { get; }
    public ConditionGroupDefinition EntryRequirements { get; }
    public DefinitionId? EntryEventId { get; }
    public DefinitionId? CompletionEventId { get; }
    public int ResetMilliseconds { get; }
    public int MinimumPartySize { get; }
    public int MaximumPartySize { get; }
    public Dictionary<string, float> Parameters { get; }
    public Dictionary<string, string> Metadata { get; }
}
