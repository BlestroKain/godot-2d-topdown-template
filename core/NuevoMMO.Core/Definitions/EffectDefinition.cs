namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de un estado/buff/debuff. El runtime conserva instancias y stacks;
/// esta Definition describe duración, modificadores y acciones de ciclo de vida.
/// </summary>
public sealed record EffectDefinition : GameDefinition
{
    public EffectDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey visualKey,
        EffectDisposition disposition = EffectDisposition.Neutral,
        EffectLifecycleDefinition? lifecycle = null,
        Dictionary<StatId, float>? flatStats = null,
        Dictionary<StatId, float>? percentStats = null,
        Dictionary<string, float>? modifiers = null,
        TechniqueActionDefinition[]? onApply = null,
        TechniqueActionDefinition[]? onTick = null,
        TechniqueActionDefinition[]? onExpire = null,
        string[]? categories = null,
        Dictionary<string, ContentKey>? visualOverrides = null,
        Dictionary<string, float>? parameters = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));

        VisualKey = visualKey;
        Disposition = disposition;
        Lifecycle = lifecycle ?? new EffectLifecycleDefinition();
        FlatStats = DefinitionModelGuards.CopyFinite(flatStats, nameof(flatStats));
        PercentStats = DefinitionModelGuards.CopyFinite(percentStats, nameof(percentStats));
        Modifiers = DefinitionModelGuards.CopyFinite(modifiers, nameof(modifiers));
        OnApply = onApply?.ToArray() ?? [];
        OnTick = onTick?.ToArray() ?? [];
        OnExpire = onExpire?.ToArray() ?? [];
        Categories = DefinitionCollectionGuards.CopyStrings(categories);
        VisualOverrides = DefinitionCollectionGuards.CopyContentKeys(visualOverrides, nameof(visualOverrides));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public ContentKey VisualKey { get; }
    public EffectDisposition Disposition { get; }
    public EffectLifecycleDefinition Lifecycle { get; }
    public Dictionary<StatId, float> FlatStats { get; }
    public Dictionary<StatId, float> PercentStats { get; }
    public Dictionary<string, float> Modifiers { get; }
    public TechniqueActionDefinition[] OnApply { get; }
    public TechniqueActionDefinition[] OnTick { get; }
    public TechniqueActionDefinition[] OnExpire { get; }
    public string[] Categories { get; }
    public Dictionary<string, ContentKey> VisualOverrides { get; }
    public Dictionary<string, float> Parameters { get; }
}
