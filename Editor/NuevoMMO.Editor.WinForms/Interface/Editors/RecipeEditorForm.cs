namespace NuevoMMO.Editor;

public partial class RecipeEditorForm : DefinitionEditorForm
{
    public RecipeEditorForm()
    {
        InitializeComponent();
    }

    public RecipeEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Recipes)
    {
        InitializeComponent();
    }
}
