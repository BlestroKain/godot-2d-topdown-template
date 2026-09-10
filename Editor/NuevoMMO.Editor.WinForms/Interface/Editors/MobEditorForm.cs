namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class MobEditorForm : DefinitionEditorForm
{
    public MobEditorForm()
    {
        InitializeComponent();
    }

    public MobEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Mobs)
    {
        InitializeComponent();
    }
}
