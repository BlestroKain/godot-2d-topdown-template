namespace NuevoMMO.Editor;

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
