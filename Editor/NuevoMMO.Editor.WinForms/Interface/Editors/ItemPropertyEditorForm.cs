namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class ItemPropertyEditorForm : DefinitionEditorForm
{
    public ItemPropertyEditorForm()
    {
        InitializeComponent();
    }

    public ItemPropertyEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.ItemProperties)
    {
        InitializeComponent();
    }
}
