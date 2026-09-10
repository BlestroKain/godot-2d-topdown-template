namespace NuevoMMO.Editor;

public partial class ResourceEditorForm : DefinitionEditorForm
{
    public ResourceEditorForm()
    {
        InitializeComponent();
    }

    public ResourceEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Resources)
    {
        InitializeComponent();
    }
}
