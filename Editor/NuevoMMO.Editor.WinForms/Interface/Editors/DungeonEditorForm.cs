namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class DungeonEditorForm : DefinitionEditorForm
{
    public DungeonEditorForm()
    {
        InitializeComponent();
    }

    public DungeonEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Dungeons)
    {
        InitializeComponent();
    }
}
