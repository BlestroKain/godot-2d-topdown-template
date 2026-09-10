namespace NuevoMMO.Core;

public sealed record ProfessionDefinition : GameDefinition
{
    public ProfessionDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags) : base(id, key, name, description, enabled, version, tags) { }
}
