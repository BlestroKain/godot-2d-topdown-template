namespace NuevoMMO.Core;

public sealed record MobDefinition : GameDefinition
{
    public MobDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, ContentKey visualKey, DefinitionId? lootTableId = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        VisualKey = visualKey;
        LootTableId = lootTableId;
    }

    public ContentKey VisualKey { get; init; }
    public DefinitionId? LootTableId { get; init; }
}
