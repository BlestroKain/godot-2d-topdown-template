namespace NuevoMMO.Editor;

public partial class ProfessionEditorForm : DefinitionEditorForm
{
    public ProfessionEditorForm()
    {
        InitializeComponent();
    }

    public ProfessionEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Professions)
    {
        InitializeComponent();
    }
}
