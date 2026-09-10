namespace NuevoMMO.Core;

public sealed record ItemDefinition : GameDefinition
{
    public ItemDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, ContentKey visualKey, DefinitionId[]? propertyIds = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        VisualKey = visualKey;
        PropertyIds = propertyIds ?? [];
    }

    public ContentKey VisualKey { get; init; }
    public DefinitionId[] PropertyIds { get; init; }
}
