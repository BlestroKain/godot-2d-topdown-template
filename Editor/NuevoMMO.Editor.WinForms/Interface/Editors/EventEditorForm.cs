namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class EventEditorForm : DefinitionEditorForm
{
    public EventEditorForm()
    {
        InitializeComponent();
    }

    public EventEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Events)
    {
        InitializeComponent();
    }
}
