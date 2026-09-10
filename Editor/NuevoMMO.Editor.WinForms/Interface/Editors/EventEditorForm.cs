namespace NuevoMMO.Editor;

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
