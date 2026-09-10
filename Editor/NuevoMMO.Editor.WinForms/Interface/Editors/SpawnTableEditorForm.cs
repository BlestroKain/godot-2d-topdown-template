namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
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
