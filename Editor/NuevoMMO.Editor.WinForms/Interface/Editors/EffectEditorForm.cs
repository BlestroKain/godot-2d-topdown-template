namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class EffectEditorForm : DefinitionEditorForm
{
    public EffectEditorForm()
    {
        InitializeComponent();
    }

    public EffectEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Effects)
    {
        InitializeComponent();
    }
}
