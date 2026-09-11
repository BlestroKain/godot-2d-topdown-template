namespace NuevoMMO.Core;

public sealed record TraditionExpressionDefinition
{
    public TraditionExpressionDefinition(
        Element element,
        DefinitionId[]? techniqueIds = null,
        ContentKey? visualKey = null,
        Dictionary<StatId, float>? modifiers = null,
        Dictionary<string, float>? parameters = null)
    {
        if (element == Element.Neutral)
            throw new ArgumentException("Una expresión de Tradición debe ser Tierra, Fuego, Aire o Agua.", nameof(element));
        if (visualKey is { } visual && visual.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));

        Element = element;
        TechniqueIds = DefinitionModelGuards.CopyIds(techniqueIds, nameof(techniqueIds));
        VisualKey = visualKey;
        Modifiers = DefinitionModelGuards.CopyFinite(modifiers, nameof(modifiers));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Element Element { get; }
    public DefinitionId[] TechniqueIds { get; }
    public ContentKey? VisualKey { get; }
    public Dictionary<StatId, float> Modifiers { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record TraditionTechniqueUnlockDefinition
{
    public TraditionTechniqueUnlockDefinition(
        DefinitionId techniqueId,
        float requiredMastery = 0,
        ConditionGroupDefinition? requirements = null)
    {
        if (techniqueId.IsEmpty) throw new ArgumentException("TechniqueId vacío.", nameof(techniqueId));
        if (!float.IsFinite(requiredMastery) || requiredMastery < 0)
            throw new ArgumentOutOfRangeException(nameof(requiredMastery));

        TechniqueId = techniqueId;
        RequiredMastery = requiredMastery;
        Requirements = requirements ?? ConditionGroupDefinition.Empty;
    }

    public DefinitionId TechniqueId { get; }
    public float RequiredMastery { get; }
    public ConditionGroupDefinition Requirements { get; }
}

public sealed record TraditionResourceDefinition
{
    public TraditionResourceDefinition(
        ContentKey key,
        string displayName,
        float baseMaximum = 0,
        float baseRegeneration = 0,
        Dictionary<string, float>? parameters = null)
    {
        if (key.IsEmpty) throw new ArgumentException("Resource key vacía.", nameof(key));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("DisplayName requerido.", nameof(displayName));
        if (!float.IsFinite(baseMaximum) || baseMaximum < 0) throw new ArgumentOutOfRangeException(nameof(baseMaximum));
        if (!float.IsFinite(baseRegeneration)) throw new ArgumentOutOfRangeException(nameof(baseRegeneration));

        Key = key;
        DisplayName = displayName.Trim();
        BaseMaximum = baseMaximum;
        BaseRegeneration = baseRegeneration;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public ContentKey Key { get; }
    public string DisplayName { get; }
    public float BaseMaximum { get; }
    public float BaseRegeneration { get; }
    public Dictionary<string, float> Parameters { get; }
}
