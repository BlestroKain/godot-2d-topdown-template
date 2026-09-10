namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class TilesetEditorForm : DefinitionEditorForm
{
    public TilesetEditorForm()
    {
        InitializeComponent();
    }

    public TilesetEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Tilesets)
    {
        InitializeComponent();
    }
}
