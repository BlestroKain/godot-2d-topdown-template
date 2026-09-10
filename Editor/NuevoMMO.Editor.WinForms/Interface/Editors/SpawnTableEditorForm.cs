namespace NuevoMMO.Editor;

public partial class SpawnTableEditorForm : DefinitionEditorForm
{
    public SpawnTableEditorForm()
    {
        InitializeComponent();
    }

    public SpawnTableEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.SpawnTables)
    {
        InitializeComponent();
    }
}
