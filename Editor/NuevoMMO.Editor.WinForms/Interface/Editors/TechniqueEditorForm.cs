namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class TechniqueEditorForm : DefinitionEditorForm
{
    public TechniqueEditorForm()
    {
        InitializeComponent();
    }

    public TechniqueEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Techniques)
    {
        InitializeComponent();
    }
}
