using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class EventEditorForm : DefinitionEditorForm
{
    private readonly List<EventPageDefinition> workingPages = [];
    private bool syncing;

    public EventEditorForm() => InitializeComponent();

    public EventEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Events)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not EventDefinition evt) return;
        syncing = true;
        SelectEnum(scopeCombo, evt.Scope);
        workingPages.Clear();
        workingPages.AddRange(evt.Pages);
        RefreshPages(workingPages.FirstOrDefault()?.Id);
        BindDefinitionCombo<ItemDefinition>(commandItemCombo, null);
        BindDefinitionCombo<TechniqueDefinition>(commandTechniqueCombo, null);
        syncing = false;
    }

    protected override void ClearSpecific()
    {
        workingPages.Clear();
        pagesList.Items.Clear();
        commandsList.Items.Clear();
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        RememberPageHeader();
        ApplyCommand();
        var evt = current as EventDefinition ?? throw new InvalidOperationException("La selección no es un Evento.");
        var scope = ReadEnum(scopeCombo, evt.Scope);
        var placement = scope == EventScope.Map ? evt.Placement : null;
        if (scope == EventScope.Map && placement is null)
            throw new InvalidOperationException("Un evento de mapa requiere Placement. Colóquelo desde el mapa.");
        return new EventDefinition(id, key, name, description, enabled, version, tags, scope, placement, workingPages.ToArray(), evt.Metadata);
    }

    private void RefreshPages(Guid? selected)
    {
        pagesList.BeginUpdate();
        pagesList.Items.Clear();
        foreach (var page in workingPages)
            pagesList.Items.Add(new PageItem(page));
        pagesList.EndUpdate();
        if (pagesList.Items.Count == 0)
        {
            commandsList.Items.Clear();
            return;
        }

        var index = workingPages.FindIndex(page => page.Id == selected);
        pagesList.SelectedIndex = index >= 0 ? index : 0;
        LoadSelectedPage();
    }

    private EventPageDefinition? SelectedPage()
        => pagesList.SelectedItem is PageItem item ? item.Page : null;

    private void LoadSelectedPage()
    {
        var page = SelectedPage();
        if (page is null) return;
        syncing = true;
        SelectEnum(triggerCombo, page.Trigger);
        SetNumeric(priorityNumeric, page.Priority);
        SetNumeric(radiusNumeric, (decimal)page.InteractionRadius);
        commandsList.BeginUpdate();
        commandsList.Items.Clear();
        if (page.CommandLists.TryGetValue(page.RootCommandListId, out var commands))
        {
            foreach (var command in commands)
                commandsList.Items.Add(new CommandItem(command));
        }
        commandsList.EndUpdate();
        if (commandsList.Items.Count > 0) commandsList.SelectedIndex = 0;
        else ClearCommandFields();
        syncing = false;
    }

    private void RememberPageHeader()
    {
        if (syncing) return;
        var page = SelectedPage();
        if (page is null) return;
        var index = workingPages.FindIndex(existing => existing.Id == page.Id);
        if (index < 0) return;
        workingPages[index] = CopyPage(
            page,
            ReadEnum(triggerCombo, page.Trigger),
            (int)priorityNumeric.Value,
            (float)radiusNumeric.Value,
            page.CommandLists);
        if (pagesList.SelectedItem is PageItem item)
            pagesList.Items[pagesList.SelectedIndex] = new PageItem(workingPages[index]);
    }

    private void AddPage()
    {
        RememberPageHeader();
        var root = Guid.NewGuid();
        var page = new EventPageDefinition(
            Guid.NewGuid(),
            EventTrigger.Action,
            commandLists: new Dictionary<Guid, EventCommandDefinition[]> { [root] = [] },
            rootCommandListId: root);
        workingPages.Add(page);
        RefreshPages(page.Id);
    }

    private void RemovePage()
    {
        var page = SelectedPage();
        if (page is null) return;
        workingPages.RemoveAll(existing => existing.Id == page.Id);
        RefreshPages(workingPages.FirstOrDefault()?.Id);
    }

    private void AddCommand()
    {
        RememberPageHeader();
        var page = SelectedPage();
        if (page is null)
        {
            AddPage();
            page = SelectedPage();
            if (page is null) return;
        }

        var kind = ReadEnum(commandKindCombo, EventCommandKind.Dialogue);
        var command = new EventCommandDefinition(Guid.NewGuid(), kind);
        var lists = page.CommandLists.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray());
        var root = page.RootCommandListId;
        lists[root] = [.. lists.GetValueOrDefault(root) ?? [], command];
        ReplaceSelectedPage(CopyPage(page, page.Trigger, page.Priority, page.InteractionRadius, lists));
        LoadSelectedPage();
        commandsList.SelectedIndex = commandsList.Items.Count - 1;
    }

    private void RemoveCommand()
    {
        var page = SelectedPage();
        if (page is null || commandsList.SelectedItem is not CommandItem selected) return;
        var lists = page.CommandLists.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray());
        foreach (var key in lists.Keys.ToArray())
            lists[key] = lists[key].Where(command => command.Id != selected.Command.Id).ToArray();
        ReplaceSelectedPage(CopyPage(page, page.Trigger, page.Priority, page.InteractionRadius, lists));
        LoadSelectedPage();
    }

    private void LoadSelectedCommand()
    {
        if (commandsList.SelectedItem is not CommandItem selected)
        {
            ClearCommandFields();
            return;
        }

        var command = selected.Command;
        SelectEnum(commandKindCombo, command.Kind);
        commandTextBox.Text = command.Text.GetValueOrDefault("text") ?? command.Text.GetValueOrDefault("key") ?? string.Empty;
        commandKeyBox.Text = command.Text.GetValueOrDefault("key") ?? string.Empty;
        SetNumeric(commandValueNumeric, (decimal)command.Numbers.GetValueOrDefault("value"));
        SetNumeric(commandXNumeric, (decimal)command.Numbers.GetValueOrDefault("x"));
        SetNumeric(commandYNumeric, (decimal)command.Numbers.GetValueOrDefault("y"));
        if (command.References.TryGetValue("item", out var item)) BindDefinitionCombo<ItemDefinition>(commandItemCombo, item);
        if (command.References.TryGetValue("technique", out var technique))
            BindDefinitionCombo<TechniqueDefinition>(commandTechniqueCombo, technique);
    }

    private void ClearCommandFields()
    {
        commandTextBox.Clear();
        commandKeyBox.Clear();
        commandValueNumeric.Value = 0;
        commandXNumeric.Value = 0;
        commandYNumeric.Value = 0;
    }

    private void ApplyCommand()
    {
        if (syncing) return;
        var page = SelectedPage();
        if (page is null || commandsList.SelectedItem is not CommandItem selected) return;
        var kind = ReadEnum(commandKindCombo, selected.Command.Kind);
        var text = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(commandTextBox.Text)) text["text"] = commandTextBox.Text;
        if (!string.IsNullOrWhiteSpace(commandKeyBox.Text)) text["key"] = commandKeyBox.Text.Trim();
        var numbers = new Dictionary<string, float>
        {
            ["value"] = (float)commandValueNumeric.Value,
            ["x"] = (float)commandXNumeric.Value,
            ["y"] = (float)commandYNumeric.Value,
            ["amount"] = (float)commandValueNumeric.Value
        };
        var references = new Dictionary<string, DefinitionId>();
        if (ReadDefinitionId(commandItemCombo) is { } item) references["item"] = item;
        if (ReadDefinitionId(commandTechniqueCombo) is { } technique) references["technique"] = technique;
        var updated = new EventCommandDefinition(selected.Command.Id, kind, references, numbers, text);
        var lists = page.CommandLists.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray());
        foreach (var key in lists.Keys.ToArray())
        {
            var commands = lists[key].ToArray();
            var index = Array.FindIndex(commands, command => command.Id == updated.Id);
            if (index < 0) continue;
            commands[index] = updated;
            lists[key] = commands;
        }

        ReplaceSelectedPage(CopyPage(page, page.Trigger, page.Priority, page.InteractionRadius, lists));
        var selectedIndex = commandsList.SelectedIndex;
        LoadSelectedPage();
        if (selectedIndex >= 0 && selectedIndex < commandsList.Items.Count)
            commandsList.SelectedIndex = selectedIndex;
    }

    private void ReplaceSelectedPage(EventPageDefinition page)
    {
        var index = workingPages.FindIndex(existing => existing.Id == page.Id);
        if (index < 0) workingPages.Add(page);
        else workingPages[index] = page;
    }

    private static EventPageDefinition CopyPage(
        EventPageDefinition page,
        EventTrigger trigger,
        int priority,
        float radius,
        Dictionary<Guid, EventCommandDefinition[]> lists)
        => new(page.Id, trigger, priority, page.Conditions, page.Visual, page.Movement, radius,
            page.FreezeDuringInteraction, lists, page.RootCommandListId, page.TriggerParameters);

    private sealed record PageItem(EventPageDefinition Page)
    {
        public override string ToString() => $"{Page.Trigger}  p{Page.Priority}  r{Page.InteractionRadius:0}";
    }

    private sealed record CommandItem(EventCommandDefinition Command)
    {
        public override string ToString()
        {
            var detail = Command.Text.GetValueOrDefault("text")
                         ?? Command.Text.GetValueOrDefault("key")
                         ?? Command.Kind.ToString();
            return $"{Command.Kind}: {detail}";
        }
    }
}
