using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class RecipeEditorForm : DefinitionEditorForm
{
    public RecipeEditorForm() => InitializeComponent();

    public RecipeEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Recipes)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not RecipeDefinition recipe) return;
        BindDefinitionCombo<ProfessionDefinition>(professionCombo, recipe.ProfessionId, optional: false);
        BindDefinitionCombo<ItemDefinition>(outputItemCombo, recipe.OutputItemId, optional: false);
        SetNumeric(outputQuantityNumeric, recipe.Outputs.GetValueOrDefault(recipe.OutputItemId, 1));
        inputItemsTextBox.Text = string.Join(Environment.NewLine, recipe.InputItemIds.Select(id =>
            Editor.Definitions.TryGet<ItemDefinition>(id, out var item) && item is not null ? item.Key.Value : id.ToString()));
        SetNumeric(requiredLevelNumeric, recipe.RequiredProfessionLevel);
        stationKeyTextBox.Text = recipe.RequiredStationKey?.Value ?? string.Empty;
        SetNumeric(craftTimeNumeric, recipe.CraftMilliseconds);
        SetNumeric(failureNumeric, (decimal)recipe.FailureChancePercent);
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var recipe = current as RecipeDefinition ?? throw new InvalidOperationException("La selección no es una Receta.");
        var inputs = ResolveDefinitionList<ItemDefinition>(inputItemsTextBox);
        var quantities = inputs.ToDictionary(itemId => itemId, itemId => recipe.Ingredients.GetValueOrDefault(itemId, 1));
        var additional = recipe.Outputs
            .Where(pair => pair.Key != recipe.OutputItemId)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        return new RecipeDefinition(
            id, key, name, description, enabled, version, tags,
            RequireDefinitionId(professionCombo, "Profesión"),
            inputs,
            RequireDefinitionId(outputItemCombo, "Resultado"),
            quantities,
            (int)outputQuantityNumeric.Value,
            additional,
            (int)requiredLevelNumeric.Value,
            ReadOptionalContentKey(stationKeyTextBox),
            (int)craftTimeNumeric.Value,
            (float)failureNumeric.Value,
            recipe.IngredientLossChancePercent,
            recipe.Requirements,
            recipe.CompletionEventId,
            recipe.OutputPropertyRanges,
            recipe.Parameters);
    }
}
