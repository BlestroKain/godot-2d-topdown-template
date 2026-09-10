namespace NuevoMMO.Editor;

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
