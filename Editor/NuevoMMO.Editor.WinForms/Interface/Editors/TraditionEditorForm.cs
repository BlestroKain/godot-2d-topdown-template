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
        ConfigureVisual(AssetKind.Entity);
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not TraditionDefinition tradition) return;
        BindVisualPicker(tradition.VisualKey ?? default);
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var tradition = current as TraditionDefinition ?? throw new InvalidOperationException("La selección no es una Tradición.");
        return new TraditionDefinition(
            id, key, name, description, enabled, version, tags,
            ReadVisualPicker(required: false) is { IsEmpty: false } visual ? visual : null,
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
