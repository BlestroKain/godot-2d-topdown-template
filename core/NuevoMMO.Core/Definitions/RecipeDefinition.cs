namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de una receta. Describe insumos, resultados, profesión,
/// requisitos, tiempos y consecuencias; el ProfessionSystem ejecuta el proceso.
/// </summary>
public sealed record RecipeDefinition : GameDefinition
{
    public RecipeDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        DefinitionId professionId,
        DefinitionId[]? inputItemIds,
        DefinitionId outputItemId,
        Dictionary<DefinitionId, int>? ingredientQuantities = null,
        int outputQuantity = 1,
        Dictionary<DefinitionId, int>? additionalOutputs = null,
        int requiredProfessionLevel = 0,
        ContentKey? requiredStationKey = null,
        int craftMilliseconds = 0,
        float failureChancePercent = 0,
        float ingredientLossChancePercent = 100,
        ConditionGroupDefinition? requirements = null,
        DefinitionId? completionEventId = null,
        Dictionary<DefinitionId, NumericRange>? outputPropertyRanges = null,
        Dictionary<string, float>? parameters = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (professionId.IsEmpty) throw new ArgumentException("ProfessionId vacío.", nameof(professionId));
        if (outputItemId.IsEmpty) throw new ArgumentException("OutputItemId vacío.", nameof(outputItemId));
        if (outputQuantity < 1) throw new ArgumentOutOfRangeException(nameof(outputQuantity));
        if (requiredProfessionLevel < 0) throw new ArgumentOutOfRangeException(nameof(requiredProfessionLevel));
        if (requiredStationKey is { } station && station.IsEmpty) throw new ArgumentException("RequiredStationKey vacío.", nameof(requiredStationKey));
        if (craftMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(craftMilliseconds));
        ValidatePercent(failureChancePercent, nameof(failureChancePercent));
        ValidatePercent(ingredientLossChancePercent, nameof(ingredientLossChancePercent));
        if (completionEventId is { } evt && evt.IsEmpty) throw new ArgumentException("CompletionEventId vacío.", nameof(completionEventId));

        var legacyInputs = inputItemIds?.ToArray() ?? [];
        if (legacyInputs.Any(static item => item.IsEmpty))
            throw new ArgumentException("InputItemIds contiene un DefinitionId vacío.", nameof(inputItemIds));

        var ingredients = ingredientQuantities is null
            ? legacyInputs.GroupBy(static item => item).ToDictionary(static group => group.Key, static group => group.Count())
            : DefinitionCollectionGuards.CopyPositiveQuantities(ingredientQuantities, nameof(ingredientQuantities));

        var outputs = DefinitionCollectionGuards.CopyPositiveQuantities(additionalOutputs, nameof(additionalOutputs));
        if (outputs.TryGetValue(outputItemId, out var extraMainQuantity))
            outputs[outputItemId] = checked(extraMainQuantity + outputQuantity);
        else
            outputs.Add(outputItemId, outputQuantity);

        ProfessionId = professionId;
        RequiredProfessionLevel = requiredProfessionLevel;
        RequiredStationKey = requiredStationKey;
        Ingredients = ingredients;
        Outputs = outputs;
        OutputItemId = outputItemId;
        InputItemIds = Ingredients.Keys.ToArray();
        CraftMilliseconds = craftMilliseconds;
        FailureChancePercent = failureChancePercent;
        IngredientLossChancePercent = ingredientLossChancePercent;
        Requirements = requirements ?? ConditionGroupDefinition.Empty;
        CompletionEventId = completionEventId;
        OutputPropertyRanges = DefinitionModelGuards.CopyRanges(outputPropertyRanges, nameof(outputPropertyRanges));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public DefinitionId ProfessionId { get; }
    public int RequiredProfessionLevel { get; }
    public ContentKey? RequiredStationKey { get; }
    public Dictionary<DefinitionId, int> Ingredients { get; }
    public Dictionary<DefinitionId, int> Outputs { get; }

    /// <summary>Vista de compatibilidad con el modelo inicial.</summary>
    public DefinitionId[] InputItemIds { get; }

    /// <summary>Resultado principal de la receta.</summary>
    public DefinitionId OutputItemId { get; }

    public int CraftMilliseconds { get; }
    public float FailureChancePercent { get; }
    public float IngredientLossChancePercent { get; }
    public ConditionGroupDefinition Requirements { get; }
    public DefinitionId? CompletionEventId { get; }
    public Dictionary<DefinitionId, NumericRange> OutputPropertyRanges { get; }
    public Dictionary<string, float> Parameters { get; }

    private static void ValidatePercent(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value is < 0 or > 100)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
