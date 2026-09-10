namespace NuevoMMO.Editor;

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
