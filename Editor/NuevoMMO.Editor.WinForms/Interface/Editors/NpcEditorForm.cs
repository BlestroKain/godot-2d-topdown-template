namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class NpcEditorForm : DefinitionEditorForm
{
    public NpcEditorForm()
    {
        InitializeComponent();
    }

    public NpcEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Npcs)
    {
        InitializeComponent();
    }
}
