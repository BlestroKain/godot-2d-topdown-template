namespace NuevoMMO.Core;

public sealed record ProfessionActivityDefinition
{
    public ProfessionActivityDefinition(
        string displayName,
        ContentKey? requiredToolKey = null,
        DefinitionId[]? techniqueIds = null,
        ConditionGroupDefinition? requirements = null,
        Dictionary<string, float>? parameters = null,
        Dictionary<string, DefinitionId>? eventHooks = null)
    {
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("DisplayName requerido.", nameof(displayName));
        if (requiredToolKey is { } tool && tool.IsEmpty) throw new ArgumentException("RequiredToolKey vacío.", nameof(requiredToolKey));

        DisplayName = displayName.Trim();
        RequiredToolKey = requiredToolKey;
        TechniqueIds = DefinitionModelGuards.CopyIds(techniqueIds, nameof(techniqueIds));
        Requirements = requirements ?? ConditionGroupDefinition.Empty;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
        EventHooks = DefinitionModelGuards.CopyDefinitionHooks(eventHooks, nameof(eventHooks));
    }

    public string DisplayName { get; }
    public ContentKey? RequiredToolKey { get; }
    public DefinitionId[] TechniqueIds { get; }
    public ConditionGroupDefinition Requirements { get; }
    public Dictionary<string, float> Parameters { get; }
    public Dictionary<string, DefinitionId> EventHooks { get; }
}

public sealed record ProfessionSpecializationDefinition
{
    public ProfessionSpecializationDefinition(
        string displayName,
        string? description = null,
        DefinitionId[]? techniqueIds = null,
        Dictionary<string, float>? modifiers = null,
        Dictionary<string, float>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("DisplayName requerido.", nameof(displayName));

        DisplayName = displayName.Trim();
        Description = description?.Trim() ?? string.Empty;
        TechniqueIds = DefinitionModelGuards.CopyIds(techniqueIds, nameof(techniqueIds));
        Modifiers = DefinitionModelGuards.CopyFinite(modifiers, nameof(modifiers));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public string DisplayName { get; }
    public string Description { get; }
    public DefinitionId[] TechniqueIds { get; }
    public Dictionary<string, float> Modifiers { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record ProfessionMasteryDefinition
{
    public ProfessionMasteryDefinition(
        Dictionary<int, long>? experienceRequirements = null,
        string[]? dimensions = null,
        Dictionary<string, float>? parameters = null)
    {
        ExperienceRequirements = DefinitionCollectionGuards.CopyExperienceCurve(experienceRequirements, nameof(experienceRequirements));
        Dimensions = DefinitionCollectionGuards.CopyStrings(dimensions);
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Dictionary<int, long> ExperienceRequirements { get; }
    public string[] Dimensions { get; }
    public Dictionary<string, float> Parameters { get; }
}
