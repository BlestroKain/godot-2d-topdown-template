namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
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
