namespace NuevoMMO.Editor;

public partial class ItemEditorForm : DefinitionEditorForm
{
    public ItemEditorForm()
    {
        InitializeComponent();
    }

    public ItemEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Items)
    {
        InitializeComponent();
    }
}
