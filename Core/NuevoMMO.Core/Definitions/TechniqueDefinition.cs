namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de una técnica. Una técnica se compone de targeting, tiempos, costes
/// y una lista ordenada de acciones; no está limitada a un único golpe de daño.
/// </summary>
public sealed record TechniqueDefinition : GameDefinition
{
    public TechniqueDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey visualKey,
        Element element = Element.Neutral,
        TechniqueTargetingDefinition? targeting = null,
        TechniqueTimingDefinition? timing = null,
        Dictionary<VitalId, float>? vitalCosts = null,
        Dictionary<string, float>? resourceCosts = null,
        TechniqueActionDefinition[]? actions = null,
        ConditionGroupDefinition? castRequirements = null,
        string? cannotCastMessage = null,
        Dictionary<string, DefinitionId>? eventHooks = null,
        Dictionary<string, ContentKey>? visualOverrides = null,
        Dictionary<string, float>? parameters = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));

        VisualKey = visualKey;
        Element = element;
        Targeting = targeting ?? new TechniqueTargetingDefinition();
        Timing = timing ?? new TechniqueTimingDefinition();
        VitalCosts = DefinitionModelGuards.CopyNonNegative(vitalCosts, nameof(vitalCosts));
        ResourceCosts = DefinitionModelGuards.CopyNonNegative(resourceCosts, nameof(resourceCosts));
        Actions = actions?.ToArray() ?? [];
        CastRequirements = castRequirements ?? ConditionGroupDefinition.Empty;
        CannotCastMessage = cannotCastMessage?.Trim() ?? string.Empty;
        EventHooks = DefinitionModelGuards.CopyDefinitionHooks(eventHooks, nameof(eventHooks));
        VisualOverrides = DefinitionCollectionGuards.CopyContentKeys(visualOverrides, nameof(visualOverrides));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public ContentKey VisualKey { get; }
    public Element Element { get; }
    public TechniqueTargetingDefinition Targeting { get; }
    public TechniqueTimingDefinition Timing { get; }
    public Dictionary<VitalId, float> VitalCosts { get; }
    public Dictionary<string, float> ResourceCosts { get; }
    public TechniqueActionDefinition[] Actions { get; }
    public ConditionGroupDefinition CastRequirements { get; }
    public string CannotCastMessage { get; }
    public Dictionary<string, DefinitionId> EventHooks { get; }
    public Dictionary<string, ContentKey> VisualOverrides { get; }
    public Dictionary<string, float> Parameters { get; }
}
