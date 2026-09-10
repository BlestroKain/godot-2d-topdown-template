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
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not ProfessionDefinition profession) return;
        visualKeyTextBox.Text = profession.VisualKey?.Value ?? string.Empty;
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
            ReadOptionalContentKey(visualKeyTextBox),
            new ProfessionMasteryDefinition(profession.Mastery.ExperienceRequirements, dimensions, profession.Mastery.Parameters),
            profession.Activities,
            profession.Specializations,
            profession.TechniqueIds,
            profession.EventHooks,
            profession.VisualOverrides,
            profession.Parameters);
    }
}
