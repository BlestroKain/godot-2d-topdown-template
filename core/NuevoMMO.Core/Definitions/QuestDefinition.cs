namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de una misión. El progreso concreto vive en el personaje;
/// esta Definition contiene oferta, requisitos, tareas, presentación y eventos.
/// </summary>
public sealed record QuestDefinition : GameDefinition
{
    public QuestDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        string? startDescription = null,
        string? beforeDescription = null,
        string? inProgressDescription = null,
        string? endDescription = null,
        bool repeatable = false,
        bool quitable = true,
        ConditionGroupDefinition? requirements = null,
        DefinitionId? startEventId = null,
        DefinitionId? endEventId = null,
        QuestTaskDefinition[]? tasks = null,
        QuestLogPresentationDefinition? logPresentation = null,
        Dictionary<string, DefinitionId>? eventHooks = null,
        Dictionary<string, float>? parameters = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (startEventId is { } start && start.IsEmpty) throw new ArgumentException("StartEventId vacío.", nameof(startEventId));
        if (endEventId is { } end && end.IsEmpty) throw new ArgumentException("EndEventId vacío.", nameof(endEventId));

        var normalizedTasks = tasks?.ToArray() ?? [];
        if (normalizedTasks.Select(static task => task.Id).Distinct().Count() != normalizedTasks.Length)
            throw new ArgumentException("Tasks contiene IDs duplicados.", nameof(tasks));

        StartDescription = startDescription?.Trim() ?? string.Empty;
        BeforeDescription = beforeDescription?.Trim() ?? string.Empty;
        InProgressDescription = inProgressDescription?.Trim() ?? string.Empty;
        EndDescription = endDescription?.Trim() ?? string.Empty;
        Repeatable = repeatable;
        Quitable = quitable;
        Requirements = requirements ?? ConditionGroupDefinition.Empty;
        StartEventId = startEventId;
        EndEventId = endEventId;
        Tasks = normalizedTasks;
        LogPresentation = logPresentation ?? new QuestLogPresentationDefinition();
        EventHooks = DefinitionModelGuards.CopyDefinitionHooks(eventHooks, nameof(eventHooks));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public string StartDescription { get; }
    public string BeforeDescription { get; }
    public string InProgressDescription { get; }
    public string EndDescription { get; }
    public bool Repeatable { get; }
    public bool Quitable { get; }
    public ConditionGroupDefinition Requirements { get; }
    public DefinitionId? StartEventId { get; }
    public DefinitionId? EndEventId { get; }
    public QuestTaskDefinition[] Tasks { get; }
    public QuestLogPresentationDefinition LogPresentation { get; }
    public Dictionary<string, DefinitionId> EventHooks { get; }
    public Dictionary<string, float> Parameters { get; }
}
