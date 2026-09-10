namespace NuevoMMO.Editor;

public partial class QuestEditorForm : DefinitionEditorForm
{
    public QuestEditorForm()
    {
        InitializeComponent();
    }

    public QuestEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Quests)
    {
        InitializeComponent();
    }
}
