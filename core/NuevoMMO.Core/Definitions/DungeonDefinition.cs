namespace NuevoMMO.Core;

public sealed record DungeonDefinition : GameDefinition
{
    public DungeonDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, DefinitionId mapId) : base(id, key, name, description, enabled, version, tags)
        => MapId = mapId;

    public DefinitionId MapId { get; init; }
}
