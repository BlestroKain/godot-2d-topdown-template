namespace NuevoMMO.Core;

public sealed record RecipeDefinition : GameDefinition
{
    public RecipeDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, DefinitionId professionId, DefinitionId[] inputItemIds, DefinitionId outputItemId)
        : base(id, key, name, description, enabled, version, tags)
    {
        ProfessionId = professionId;
        InputItemIds = inputItemIds ?? [];
        OutputItemId = outputItemId;
    }

    public DefinitionId ProfessionId { get; init; }
    public DefinitionId[] InputItemIds { get; init; }
    public DefinitionId OutputItemId { get; init; }
}
