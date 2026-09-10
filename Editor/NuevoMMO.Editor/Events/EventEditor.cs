using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Coordinador de edición de EventDefinition. Mantiene un documento de trabajo y usa
/// EditorHistory para operaciones estructurales sobre páginas/comandos.
/// </summary>
public sealed class EventEditor
{
    private readonly EventDefinitionEditor definitions;
    private readonly EditorHistory history;
    private readonly DirtyState dirty;

    public EventEditor(DefinitionRegistry registry, EditorHistory history, DirtyState dirty)
    {
        definitions = new EventDefinitionEditor(registry);
        this.history = history ?? throw new ArgumentNullException(nameof(history));
        this.dirty = dirty ?? throw new ArgumentNullException(nameof(dirty));
    }

    public EventDocument? Document { get; private set; }
    public Guid? SelectedPageId { get; private set; }

    public EventDocument Open(DefinitionId id) => Open(definitions.Edit(id));

    public EventDocument Open(EventDefinition definition)
    {
        Document = EventDocument.FromDefinition(definition);
        SelectedPageId = Document.Pages.FirstOrDefault()?.Id;
        return Document;
    }

    public void Close()
    {
        Document = null;
        SelectedPageId = null;
    }

    public EventDefinition Save()
    {
        var definition = RequireDocument().ToDefinition();
        definitions.Save(definition);
        dirty.Clear();
        return definition;
    }

    public EventPageDefinition AddPage(EventTrigger trigger = EventTrigger.Action)
    {
        var document = RequireDocument();
        var rootListId = Guid.NewGuid();
        var page = new EventPageDefinition(
            Guid.NewGuid(),
            trigger,
            commandLists: new Dictionary<Guid, EventCommandDefinition[]> { [rootListId] = [] },
            rootCommandListId: rootListId);

        history.Push(new ChangeSet(
            "Agregar página de evento",
            () =>
            {
                document.Pages.Add(page);
                SelectedPageId = page.Id;
                dirty.Mark();
            },
            () =>
            {
                document.Pages.RemoveAll(existing => existing.Id == page.Id);
                SelectedPageId = document.Pages.FirstOrDefault()?.Id;
                dirty.Mark();
            }));

        return page;
    }

    public bool RemovePage(Guid pageId)
    {
        var document = RequireDocument();
        var index = document.Pages.FindIndex(page => page.Id == pageId);
        if (index < 0) return false;
        var removed = document.Pages[index];

        history.Push(new ChangeSet(
            "Eliminar página de evento",
            () =>
            {
                document.Pages.RemoveAll(page => page.Id == pageId);
                if (SelectedPageId == pageId) SelectedPageId = document.Pages.FirstOrDefault()?.Id;
                dirty.Mark();
            },
            () =>
            {
                document.Pages.Insert(Math.Min(index, document.Pages.Count), removed);
                SelectedPageId = removed.Id;
                dirty.Mark();
            }));
        return true;
    }

    public void SelectPage(Guid pageId)
    {
        _ = RequirePage(pageId);
        SelectedPageId = pageId;
    }

    public EventPageDefinition SelectedPage()
        => RequirePage(SelectedPageId ?? throw new InvalidOperationException("No hay una página seleccionada."));

    public void ReplacePage(EventPageDefinition page)
    {
        ArgumentNullException.ThrowIfNull(page);
        var document = RequireDocument();
        var index = document.Pages.FindIndex(existing => existing.Id == page.Id);
        if (index < 0) throw new KeyNotFoundException($"No existe la página {page.Id}.");
        var previous = document.Pages[index];

        history.Push(new ChangeSet(
            "Editar página de evento",
            () =>
            {
                document.Pages[index] = page;
                SelectedPageId = page.Id;
                dirty.Mark();
            },
            () =>
            {
                document.Pages[index] = previous;
                SelectedPageId = previous.Id;
                dirty.Mark();
            }));
    }

    public Guid AddCommandList(Guid pageId)
    {
        var page = RequirePage(pageId);
        var listId = Guid.NewGuid();
        var lists = CloneCommandLists(page);
        lists.Add(listId, []);
        ReplacePage(CopyPage(page, commandLists: lists));
        return listId;
    }

    public EventCommandDefinition AddCommand(Guid pageId, Guid listId, EventCommandKind kind)
    {
        var command = new EventCommandDefinition(Guid.NewGuid(), kind);
        return AddCommand(pageId, listId, command);
    }

    public EventCommandDefinition AddCommand(Guid pageId, Guid listId, EventCommandDefinition command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var page = RequirePage(pageId);
        var lists = CloneCommandLists(page);
        if (!lists.TryGetValue(listId, out var commands))
            throw new KeyNotFoundException($"No existe la lista de comandos {listId}.");
        if (lists.Values.SelectMany(static values => values).Any(existing => existing.Id == command.Id))
            throw new InvalidOperationException($"Ya existe el comando {command.Id} en la página.");

        lists[listId] = [.. commands, command];
        ReplacePage(CopyPage(page, commandLists: lists));
        return command;
    }

    public bool RemoveCommand(Guid pageId, Guid commandId)
    {
        var page = RequirePage(pageId);
        var lists = CloneCommandLists(page);
        var found = false;
        foreach (var listId in lists.Keys.ToArray())
        {
            var filtered = lists[listId].Where(command => command.Id != commandId).ToArray();
            if (filtered.Length == lists[listId].Length) continue;
            lists[listId] = filtered;
            found = true;
        }
        if (!found) return false;
        ReplacePage(CopyPage(page, commandLists: lists));
        return true;
    }

    public void ReplaceCommand(Guid pageId, EventCommandDefinition command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var page = RequirePage(pageId);
        var lists = CloneCommandLists(page);
        var found = false;
        foreach (var listId in lists.Keys.ToArray())
        {
            var commands = lists[listId].ToArray();
            var index = Array.FindIndex(commands, existing => existing.Id == command.Id);
            if (index < 0) continue;
            commands[index] = command;
            lists[listId] = commands;
            found = true;
            break;
        }
        if (!found) throw new KeyNotFoundException($"No existe el comando {command.Id}.");
        ReplacePage(CopyPage(page, commandLists: lists));
    }

    private EventDocument RequireDocument()
        => Document ?? throw new InvalidOperationException("No hay evento abierto.");

    private EventPageDefinition RequirePage(Guid pageId)
        => RequireDocument().Pages.FirstOrDefault(page => page.Id == pageId)
           ?? throw new KeyNotFoundException($"No existe la página {pageId}.");

    private static Dictionary<Guid, EventCommandDefinition[]> CloneCommandLists(EventPageDefinition page)
        => page.CommandLists.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray());

    private static EventPageDefinition CopyPage(
        EventPageDefinition page,
        Dictionary<Guid, EventCommandDefinition[]>? commandLists = null)
        => new(
            page.Id,
            page.Trigger,
            page.Priority,
            page.Conditions,
            page.Visual,
            page.Movement,
            page.InteractionRadius,
            page.FreezeDuringInteraction,
            commandLists ?? CloneCommandLists(page),
            page.RootCommandListId,
            page.TriggerParameters);
}
