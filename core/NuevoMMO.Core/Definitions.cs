namespace NuevoMMO.Core;

public abstract record GameDefinition
{
    protected GameDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version, string[]? tags)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("DefinitionId vacío.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nombre de presentación vacío.", nameof(name));
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        Id = id; Key = key; Name = name.Trim(); Description = description?.Trim() ?? string.Empty;
        Enabled = enabled; Version = version; Tags = tags?.Distinct(StringComparer.Ordinal).ToArray() ?? [];
    }

    public DefinitionId Id { get; init; }
    public ContentKey Key { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public bool Enabled { get; init; }
    public int Version { get; init; }
    public string[] Tags { get; init; }
}

public sealed record MapDefinition : GameDefinition
{
    public MapDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, ContentKey visualKey, BoundsData bounds, Vector2Data spawn, Vector2IntData tileSize,
        Vector2IntData[]? blockedCells = null) : base(id, key, name, description, enabled, version, tags)
    {
        if (!bounds.IsValid || !spawn.IsFinite || bounds.Clamp(spawn) != spawn || tileSize.X <= 0 || tileSize.Y <= 0)
            throw new ArgumentException("Geometría de mapa inválida.");
        VisualKey = visualKey; Bounds = bounds; Spawn = spawn; TileSize = tileSize;
        BlockedCells = blockedCells?.Distinct().ToArray() ?? [];
    }
    public ContentKey VisualKey { get; init; }
    public BoundsData Bounds { get; init; }
    public Vector2Data Spawn { get; init; }
    public Vector2IntData TileSize { get; init; }
    public Vector2IntData[] BlockedCells { get; init; }
}

public sealed record MobDefinition : GameDefinition
{
    public MobDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, ContentKey visualKey) : base(id, key, name, description, enabled, version, tags) => VisualKey = visualKey;
    public ContentKey VisualKey { get; init; }
}

public sealed record ItemDefinition : GameDefinition
{
    public ItemDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, ContentKey visualKey) : base(id, key, name, description, enabled, version, tags) => VisualKey = visualKey;
    public ContentKey VisualKey { get; init; }
}

public sealed record EffectDefinition(DefinitionId Id, ContentKey Key, string Name, ContentKey VisualKey);
public sealed record TechniqueDefinition(DefinitionId Id, ContentKey Key, string Name, ContentKey VisualKey);
public sealed record NpcDefinition(DefinitionId Id, ContentKey Key, string Name, ContentKey VisualKey);
public sealed record ResourceDefinition(DefinitionId Id, ContentKey Key, string Name, ContentKey VisualKey);
