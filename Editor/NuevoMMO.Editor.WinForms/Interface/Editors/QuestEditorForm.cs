using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class QuestEditorForm : DefinitionEditorForm
{
    public QuestEditorForm() => InitializeComponent();

    public QuestEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Quests)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not QuestDefinition quest) return;
        startTextBox.Text = quest.StartDescription;
        inProgressTextBox.Text = quest.InProgressDescription;
        endTextBox.Text = quest.EndDescription;
        repeatableCheck.Checked = quest.Repeatable;
        quitableCheck.Checked = quest.Quitable;
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var quest = current as QuestDefinition ?? throw new InvalidOperationException("La selección no es una Quest.");
        return new QuestDefinition(
            id, key, name, description, enabled, version, tags,
            startTextBox.Text,
            quest.BeforeDescription,
            inProgressTextBox.Text,
            endTextBox.Text,
            repeatableCheck.Checked,
            quitableCheck.Checked,
            quest.Requirements,
            quest.StartEventId,
            quest.EndEventId,
            quest.Tasks,
            quest.LogPresentation,
            quest.EventHooks,
            quest.Parameters);
    }
}
