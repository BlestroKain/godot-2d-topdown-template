namespace NuevoMMO.Editor;

public partial class LootTableEditorForm : DefinitionEditorForm
{
    public LootTableEditorForm()
    {
        InitializeComponent();
    }

    public LootTableEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.LootTables)
    {
        InitializeComponent();
    }
}
