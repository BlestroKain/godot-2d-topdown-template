using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class TraditionEditorForm : DefinitionEditorForm
{
    public TraditionEditorForm() => InitializeComponent();

    public TraditionEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Traditions)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not TraditionDefinition tradition) return;
        visualKeyTextBox.Text = tradition.VisualKey?.Value ?? string.Empty;
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var tradition = current as TraditionDefinition ?? throw new InvalidOperationException("La selección no es una Tradición.");
        return new TraditionDefinition(
            id, key, name, description, enabled, version, tags,
            ReadOptionalContentKey(visualKeyTextBox),
            tradition.Resource,
            tradition.Expressions,
            tradition.TechniqueUnlocks,
            tradition.BaseStats,
            tradition.BaseVitals,
            tradition.MasteryRequirements,
            tradition.EventHooks,
            tradition.VisualOverrides,
            tradition.Parameters);
    }
}
