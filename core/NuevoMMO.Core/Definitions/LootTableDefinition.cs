namespace NuevoMMO.Core;

public sealed record LootTableDefinition : GameDefinition
{
    public LootTableDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, DefinitionId[] itemIds) : base(id, key, name, description, enabled, version, tags)
        => ItemIds = itemIds ?? [];

    public DefinitionId[] ItemIds { get; init; }
}
