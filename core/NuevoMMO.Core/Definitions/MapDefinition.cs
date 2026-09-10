namespace NuevoMMO.Core;

public sealed record MapDefinition : GameDefinition
{
    public MapDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, ContentKey visualKey, BoundsData bounds, Vector2Data spawn, Vector2IntData tileSize,
        Vector2IntData[]? blockedCells = null) : base(id, key, name, description, enabled, version, tags)
    {
        if (!bounds.IsValid || !spawn.IsFinite || bounds.Clamp(spawn) != spawn || tileSize.X <= 0 || tileSize.Y <= 0)
            throw new ArgumentException("Geometría de mapa inválida.");
        VisualKey = visualKey;
        Bounds = bounds;
        Spawn = spawn;
        TileSize = tileSize;
        BlockedCells = blockedCells?.Distinct().ToArray() ?? [];
    }

    public MapId MapId => new(Id.Value);
    public ContentKey VisualKey { get; init; }
    public BoundsData Bounds { get; init; }
    public Vector2Data Spawn { get; init; }
    public Vector2IntData TileSize { get; init; }
    public Vector2IntData[] BlockedCells { get; init; }
}
