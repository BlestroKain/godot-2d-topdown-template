using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class ProfessionEditorForm : DefinitionEditorForm
{
    public ProfessionEditorForm() => InitializeComponent();

    public ProfessionEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Professions)
    {
        InitializeComponent();
        ConfigureVisual(AssetKind.Gui);
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not ProfessionDefinition profession) return;
        BindVisualPicker(profession.VisualKey ?? default);
        dimensionsTextBox.Text = string.Join(", ", profession.Mastery.Dimensions);
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var profession = current as ProfessionDefinition ?? throw new InvalidOperationException("La selección no es una Profesión.");
        var dimensions = dimensionsTextBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new ProfessionDefinition(
            id, key, name, description, enabled, version, tags,
            ReadVisualPicker(required: false) is { IsEmpty: false } visual ? visual : null,
            new ProfessionMasteryDefinition(profession.Mastery.ExperienceRequirements, dimensions, profession.Mastery.Parameters),
            profession.Activities,
            profession.Specializations,
            profession.TechniqueIds,
            profession.EventHooks,
            profession.VisualOverrides,
            profession.Parameters);
    }
}
