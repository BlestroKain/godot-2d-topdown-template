namespace NuevoMMO.Editor;

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
