namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
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
