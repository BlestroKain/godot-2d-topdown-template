namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de una Tradición. La Tradición describe identidad, recurso propio,
/// expresiones elementales y acceso a técnicas; el personaje conserva su maestría runtime.
/// </summary>
public sealed record TraditionDefinition : GameDefinition
{
    private static readonly Element[] CanonicalExpressionElements =
        [Element.Earth, Element.Fire, Element.Air, Element.Water];

    public TraditionDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey? visualKey = null,
        TraditionResourceDefinition? resource = null,
        Dictionary<Element, TraditionExpressionDefinition>? expressions = null,
        TraditionTechniqueUnlockDefinition[]? techniqueUnlocks = null,
        Dictionary<StatId, float>? baseStats = null,
        Dictionary<VitalId, float>? baseVitals = null,
        Dictionary<int, long>? masteryRequirements = null,
        Dictionary<string, DefinitionId>? eventHooks = null,
        Dictionary<string, ContentKey>? visualOverrides = null,
        Dictionary<string, float>? parameters = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey is { } visual && visual.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));

        var normalizedExpressions = expressions is null
            ? new Dictionary<Element, TraditionExpressionDefinition>()
            : new Dictionary<Element, TraditionExpressionDefinition>(expressions);

        if (normalizedExpressions.Any(static pair => pair.Key == Element.Neutral || pair.Value.Element != pair.Key))
            throw new ArgumentException("Expressions contiene una clave elemental inválida o no coincide con su valor.", nameof(expressions));
        if (normalizedExpressions.Count > 0 &&
            (normalizedExpressions.Count != CanonicalExpressionElements.Length ||
             CanonicalExpressionElements.Any(element => !normalizedExpressions.ContainsKey(element))))
            throw new ArgumentException("Una Tradición con expresiones debe definir Tierra, Fuego, Aire y Agua.", nameof(expressions));

        var unlocks = techniqueUnlocks?.ToArray() ?? [];
        if (unlocks.Select(static unlock => unlock.TechniqueId).Distinct().Count() != unlocks.Length)
            throw new ArgumentException("TechniqueUnlocks contiene técnicas duplicadas.", nameof(techniqueUnlocks));

        VisualKey = visualKey;
        Resource = resource;
        Expressions = normalizedExpressions;
        TechniqueUnlocks = unlocks;
        BaseStats = DefinitionModelGuards.CopyFinite(baseStats, nameof(baseStats));
        BaseVitals = DefinitionModelGuards.CopyNonNegative(baseVitals, nameof(baseVitals));
        MasteryRequirements = DefinitionCollectionGuards.CopyExperienceCurve(masteryRequirements, nameof(masteryRequirements));
        EventHooks = DefinitionModelGuards.CopyDefinitionHooks(eventHooks, nameof(eventHooks));
        VisualOverrides = DefinitionCollectionGuards.CopyContentKeys(visualOverrides, nameof(visualOverrides));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public ContentKey? VisualKey { get; }
    public TraditionResourceDefinition? Resource { get; }
    public Dictionary<Element, TraditionExpressionDefinition> Expressions { get; }
    public TraditionTechniqueUnlockDefinition[] TechniqueUnlocks { get; }
    public Dictionary<StatId, float> BaseStats { get; }
    public Dictionary<VitalId, float> BaseVitals { get; }
    public Dictionary<int, long> MasteryRequirements { get; }
    public Dictionary<string, DefinitionId> EventHooks { get; }
    public Dictionary<string, ContentKey> VisualOverrides { get; }
    public Dictionary<string, float> Parameters { get; }
}
