namespace NuevoMMO.Core;

public enum SpawnEntityKind : byte
{
    Mob,
    Npc,
    Resource
}

public enum SpawnSelectionMode : byte
{
    WeightedRandom,
    Sequence,
    All
}

public sealed record SpawnEntryDefinition
{
    public SpawnEntryDefinition(
        SpawnEntityKind kind,
        DefinitionId definitionId,
        float weight = 1,
        int minimumAlive = 0,
        int maximumAlive = 1,
        int minimumGroupSize = 1,
        int maximumGroupSize = 1,
        int minimumRespawnMilliseconds = 0,
        int maximumRespawnMilliseconds = 0,
        ConditionGroupDefinition? conditions = null,
        Dictionary<string, float>? parameters = null)
    {
        if (definitionId.IsEmpty) throw new ArgumentException("DefinitionId vacío.", nameof(definitionId));
        if (!float.IsFinite(weight) || weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
        if (minimumAlive < 0) throw new ArgumentOutOfRangeException(nameof(minimumAlive));
        if (maximumAlive < minimumAlive) throw new ArgumentOutOfRangeException(nameof(maximumAlive));
        if (minimumGroupSize < 1) throw new ArgumentOutOfRangeException(nameof(minimumGroupSize));
        if (maximumGroupSize < minimumGroupSize) throw new ArgumentOutOfRangeException(nameof(maximumGroupSize));
        if (minimumRespawnMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(minimumRespawnMilliseconds));
        if (maximumRespawnMilliseconds < minimumRespawnMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(maximumRespawnMilliseconds));

        Kind = kind;
        DefinitionId = definitionId;
        Weight = weight;
        MinimumAlive = minimumAlive;
        MaximumAlive = maximumAlive;
        MinimumGroupSize = minimumGroupSize;
        MaximumGroupSize = maximumGroupSize;
        MinimumRespawnMilliseconds = minimumRespawnMilliseconds;
        MaximumRespawnMilliseconds = maximumRespawnMilliseconds;
        Conditions = conditions ?? ConditionGroupDefinition.Empty;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public SpawnEntityKind Kind { get; }
    public DefinitionId DefinitionId { get; }
    public float Weight { get; }
    public int MinimumAlive { get; }
    public int MaximumAlive { get; }
    public int MinimumGroupSize { get; }
    public int MaximumGroupSize { get; }
    public int MinimumRespawnMilliseconds { get; }
    public int MaximumRespawnMilliseconds { get; }
    public ConditionGroupDefinition Conditions { get; }
    public Dictionary<string, float> Parameters { get; }
}

public enum DungeonInstanceMode : byte
{
    OpenWorld,
    SharedInstance,
    PartyInstance,
    SoloInstance
}

public sealed record DungeonStageDefinition
{
    public DungeonStageDefinition(
        Guid id,
        string name,
        DefinitionId mapId,
        DefinitionId[]? spawnTableIds = null,
        DefinitionId[]? eventIds = null,
        DefinitionId[]? bossMobIds = null,
        ConditionGroupDefinition? completionConditions = null,
        DefinitionId? completionEventId = null,
        Dictionary<string, float>? parameters = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("DungeonStage ID vacío.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name requerido.", nameof(name));
        if (mapId.IsEmpty) throw new ArgumentException("MapId vacío.", nameof(mapId));
        if (completionEventId is { } evt && evt.IsEmpty) throw new ArgumentException("CompletionEventId vacío.", nameof(completionEventId));

        Id = id;
        Name = name.Trim();
        MapId = mapId;
        SpawnTableIds = DefinitionModelGuards.CopyIds(spawnTableIds, nameof(spawnTableIds));
        EventIds = DefinitionModelGuards.CopyIds(eventIds, nameof(eventIds));
        BossMobIds = DefinitionModelGuards.CopyIds(bossMobIds, nameof(bossMobIds));
        CompletionConditions = completionConditions ?? ConditionGroupDefinition.Empty;
        CompletionEventId = completionEventId;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Guid Id { get; }
    public string Name { get; }
    public DefinitionId MapId { get; }
    public DefinitionId[] SpawnTableIds { get; }
    public DefinitionId[] EventIds { get; }
    public DefinitionId[] BossMobIds { get; }
    public ConditionGroupDefinition CompletionConditions { get; }
    public DefinitionId? CompletionEventId { get; }
    public Dictionary<string, float> Parameters { get; }
}
