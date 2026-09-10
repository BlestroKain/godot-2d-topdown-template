using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class EventEditorForm : DefinitionEditorForm
{
    public EventEditorForm() => InitializeComponent();

    public EventEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Events)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not EventDefinition evt) return;
        SelectEnum(scopeCombo, evt.Scope);
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var evt = current as EventDefinition ?? throw new InvalidOperationException("La selección no es un Evento.");
        var scope = ReadEnum(scopeCombo, evt.Scope);
        var placement = scope == EventScope.Map ? evt.Placement : null;
        if (scope == EventScope.Map && placement is null)
            throw new InvalidOperationException("Un evento de mapa requiere Placement. Ábralo desde el mapa o use JSON avanzado.");
        return new EventDefinition(id, key, name, description, enabled, version, tags, scope, placement, evt.Pages, evt.Metadata);
    }
}
