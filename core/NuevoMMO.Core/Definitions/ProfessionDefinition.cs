namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de una profesión. La progresión runtime pertenece al personaje;
/// aquí viven actividades, maestría, técnicas y especializaciones editables.
/// </summary>
public sealed record ProfessionDefinition : GameDefinition
{
    public ProfessionDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey? visualKey = null,
        ProfessionMasteryDefinition? mastery = null,
        Dictionary<string, ProfessionActivityDefinition>? activities = null,
        Dictionary<string, ProfessionSpecializationDefinition>? specializations = null,
        DefinitionId[]? techniqueIds = null,
        Dictionary<string, DefinitionId>? eventHooks = null,
        Dictionary<string, ContentKey>? visualOverrides = null,
        Dictionary<string, float>? parameters = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey is { } visual && visual.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));

        VisualKey = visualKey;
        Mastery = mastery ?? new ProfessionMasteryDefinition();
        Activities = CopyActivities(activities, nameof(activities));
        Specializations = CopySpecializations(specializations, nameof(specializations));
        TechniqueIds = DefinitionModelGuards.CopyIds(techniqueIds, nameof(techniqueIds));
        EventHooks = DefinitionModelGuards.CopyDefinitionHooks(eventHooks, nameof(eventHooks));
        VisualOverrides = DefinitionCollectionGuards.CopyContentKeys(visualOverrides, nameof(visualOverrides));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public ContentKey? VisualKey { get; }
    public ProfessionMasteryDefinition Mastery { get; }
    public Dictionary<string, ProfessionActivityDefinition> Activities { get; }
    public Dictionary<string, ProfessionSpecializationDefinition> Specializations { get; }
    public DefinitionId[] TechniqueIds { get; }
    public Dictionary<string, DefinitionId> EventHooks { get; }
    public Dictionary<string, ContentKey> VisualOverrides { get; }
    public Dictionary<string, float> Parameters { get; }

    private static Dictionary<string, ProfessionActivityDefinition> CopyActivities(
        Dictionary<string, ProfessionActivityDefinition>? source,
        string parameterName)
    {
        var result = new Dictionary<string, ProfessionActivityDefinition>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return result;
        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Activity key vacía.", parameterName);
            result.Add(key.Trim(), value ?? throw new ArgumentException("Activity nula.", parameterName));
        }
        return result;
    }

    private static Dictionary<string, ProfessionSpecializationDefinition> CopySpecializations(
        Dictionary<string, ProfessionSpecializationDefinition>? source,
        string parameterName)
    {
        var result = new Dictionary<string, ProfessionSpecializationDefinition>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return result;
        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Specialization key vacía.", parameterName);
            result.Add(key.Trim(), value ?? throw new ArgumentException("Specialization nula.", parameterName));
        }
        return result;
    }
}
