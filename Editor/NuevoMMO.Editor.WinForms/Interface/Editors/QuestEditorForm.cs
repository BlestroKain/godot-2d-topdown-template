namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
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
