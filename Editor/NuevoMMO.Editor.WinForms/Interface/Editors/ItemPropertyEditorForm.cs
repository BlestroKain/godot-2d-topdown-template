using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class ItemPropertyEditorForm : DefinitionEditorForm
{
    public ItemPropertyEditorForm() => InitializeComponent();

    public ItemPropertyEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.ItemProperties)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not ItemPropertyDefinition property) return;
        SetNumeric(minimumNumeric, (decimal)property.DefaultRange.Minimum);
        SetNumeric(maximumNumeric, (decimal)property.DefaultRange.Maximum);
        SelectEnum(modifierCombo, property.ModifierType);
        if (property.StatId is { } stat) SelectEnum(statCombo, stat);
        appliesItemsCheck.Checked = property.AppliesToItems;
        appliesResourcesCheck.Checked = property.AppliesToResources;
        unitTextBox.Text = property.Unit;
        SetNumeric(precisionNumeric, property.DisplayPrecision);
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var property = current as ItemPropertyDefinition ?? throw new InvalidOperationException("La selección no es una Propiedad.");
        return new ItemPropertyDefinition(
            id, key, name, description, enabled, version, tags,
            (float)minimumNumeric.Value,
            (float)maximumNumeric.Value,
            ReadEnum(modifierCombo, property.ModifierType),
            ReadEnum(statCombo, property.StatId ?? StatId.Strength),
            appliesItemsCheck.Checked,
            appliesResourcesCheck.Checked,
            unitTextBox.Text,
            (int)precisionNumeric.Value,
            property.Metadata);
    }
}
