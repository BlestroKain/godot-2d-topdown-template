namespace NuevoMMO.Core;

public sealed record SpawnTableDefinition : GameDefinition
{
    public SpawnTableDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, DefinitionId[] mobIds) : base(id, key, name, description, enabled, version, tags)
        => MobIds = mobIds ?? [];

    public DefinitionId[] MobIds { get; init; }
}
