namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class TraditionEditorForm : DefinitionEditorForm
{
    public TraditionEditorForm()
    {
        InitializeComponent();
    }

    public TraditionEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Traditions)
    {
        InitializeComponent();
    }
}
