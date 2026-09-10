namespace NuevoMMO.Core;

public sealed record ItemCombatDefinition
{
    public ItemCombatDefinition(
        float baseDamage = 0,
        Element damageElement = Element.Neutral,
        float criticalChancePercent = 0,
        float criticalMultiplier = 1.5f,
        int attackIntervalMilliseconds = 0,
        float blockChancePercent = 0,
        float blockAmountPercent = 0,
        float blockAbsorptionPercent = 0,
        Dictionary<StatId, float>? scaling = null,
        Dictionary<string, float>? parameters = null)
    {
        if (!float.IsFinite(baseDamage) || baseDamage < 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
        ValidatePercent(criticalChancePercent, nameof(criticalChancePercent));
        if (!float.IsFinite(criticalMultiplier) || criticalMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(criticalMultiplier));
        if (attackIntervalMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(attackIntervalMilliseconds));
        ValidatePercent(blockChancePercent, nameof(blockChancePercent));
        ValidatePercent(blockAmountPercent, nameof(blockAmountPercent));
        ValidatePercent(blockAbsorptionPercent, nameof(blockAbsorptionPercent));

        BaseDamage = baseDamage;
        DamageElement = damageElement;
        CriticalChancePercent = criticalChancePercent;
        CriticalMultiplier = criticalMultiplier;
        AttackIntervalMilliseconds = attackIntervalMilliseconds;
        BlockChancePercent = blockChancePercent;
        BlockAmountPercent = blockAmountPercent;
        BlockAbsorptionPercent = blockAbsorptionPercent;
        Scaling = DefinitionModelGuards.CopyFinite(scaling, nameof(scaling));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public float BaseDamage { get; }
    public Element DamageElement { get; }
    public float CriticalChancePercent { get; }
    public float CriticalMultiplier { get; }
    public int AttackIntervalMilliseconds { get; }
    public float BlockChancePercent { get; }
    public float BlockAmountPercent { get; }
    public float BlockAbsorptionPercent { get; }
    public Dictionary<StatId, float> Scaling { get; }
    public Dictionary<string, float> Parameters { get; }

    private static void ValidatePercent(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value is < 0 or > 100)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}

public sealed record ItemConsumableDefinition
{
    public ItemConsumableDefinition(
        Dictionary<VitalId, float>? flatVitals = null,
        Dictionary<VitalId, float>? percentVitals = null,
        Dictionary<VitalId, float>? vitalRegeneration = null,
        DefinitionId[]? effectIds = null,
        Dictionary<string, float>? parameters = null)
    {
        FlatVitals = DefinitionModelGuards.CopyFinite(flatVitals, nameof(flatVitals));
        PercentVitals = DefinitionModelGuards.CopyFinite(percentVitals, nameof(percentVitals));
        VitalRegeneration = DefinitionModelGuards.CopyFinite(vitalRegeneration, nameof(vitalRegeneration));
        EffectIds = DefinitionModelGuards.CopyIds(effectIds, nameof(effectIds));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Dictionary<VitalId, float> FlatVitals { get; }
    public Dictionary<VitalId, float> PercentVitals { get; }
    public Dictionary<VitalId, float> VitalRegeneration { get; }
    public DefinitionId[] EffectIds { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record ItemRequirementDefinition
{
    public ItemRequirementDefinition(
        Dictionary<StatId, float>? minimumStats = null,
        Dictionary<string, float>? parameters = null,
        string? cannotUseMessage = null)
    {
        MinimumStats = DefinitionModelGuards.CopyFinite(minimumStats, nameof(minimumStats));
        if (MinimumStats.Values.Any(static value => value < 0))
            throw new ArgumentException("Los requisitos mínimos no pueden ser negativos.", nameof(minimumStats));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
        CannotUseMessage = cannotUseMessage?.Trim() ?? string.Empty;
    }

    public Dictionary<StatId, float> MinimumStats { get; }
    public Dictionary<string, float> Parameters { get; }
    public string CannotUseMessage { get; }
}
