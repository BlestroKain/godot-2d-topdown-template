namespace NuevoMMO.Core;

public sealed record QuestDefinition : GameDefinition
{
    public QuestDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags) : base(id, key, name, description, enabled, version, tags) { }
}
