namespace NuevoMMO.Core;

public sealed record NpcDefinition : GameDefinition
{
    public NpcDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, ContentKey visualKey) : base(id, key, name, description, enabled, version, tags)
        => VisualKey = visualKey;

    public ContentKey VisualKey { get; init; }
}
