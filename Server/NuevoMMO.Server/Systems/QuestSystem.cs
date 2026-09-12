using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public sealed record QuestTaskAdvance(DefinitionId QuestId, Guid TaskId, int Previous, int Current, int Required);

/// <summary>
/// Autoridad única del estado de quests. Eventos, combate, gathering y otros sistemas solicitan
/// transiciones aquí; no mutan el QuestJournal directamente.
/// </summary>
public sealed class QuestSystem
{
    private readonly DefinitionRegistry definitions;
    private readonly ConditionSystem conditions;

    public QuestSystem(DefinitionRegistry definitions, ConditionSystem conditions)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
    }

    public QuestProgress Start(Player player, DefinitionId questId, ConditionEvaluationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        var definition = RequireQuest(questId);
        if (!definition.Enabled) throw new InvalidOperationException("La misión está deshabilitada.");

        var state = player.Quests.StateOf(questId);
        if (state == QuestRuntimeState.Active) throw new InvalidOperationException("La misión ya está activa.");
        if (state == QuestRuntimeState.Completed && !definition.Repeatable)
            throw new InvalidOperationException("La misión ya fue completada y no es repetible.");
        if (state == QuestRuntimeState.Failed)
            throw new InvalidOperationException("La misión fallida no puede reiniciarse sin una regla explícita.");
        if (!conditions.Evaluate(player, definition.Requirements, context))
            throw new InvalidOperationException("No se cumplen los requisitos de la misión.");

        var progress = player.Quests.Begin(definition);
        player.MarkDirty();
        return progress;
    }

    public QuestProgress Advance(
        Player player,
        DefinitionId questId,
        Guid? taskId = null,
        int amount = 1,
        ConditionEvaluationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        var definition = RequireQuest(questId);
        var progress = RequireActive(player, questId);

        QuestTaskDefinition task;
        if (taskId is { } explicitTask)
        {
            task = definition.Tasks.FirstOrDefault(candidate => candidate.Id == explicitTask)
                ?? throw new KeyNotFoundException("La tarea no pertenece a la misión.");
        }
        else
        {
            task = definition.Tasks.FirstOrDefault(candidate => progress.ProgressOf(candidate.Id) < candidate.Quantity)
                ?? throw new InvalidOperationException("La misión no tiene tareas pendientes.");
        }

        progress.Advance(task, amount);
        player.MarkDirty();
        return progress;
    }

    /// <summary>
    /// Observa un hecho ya resuelto por otro sistema (kill, gathering, interacción, crafting, etc.)
    /// y traduce ese hecho a progreso de tareas compatibles. No altera el sistema productor del hecho.
    /// </summary>
    public IReadOnlyList<QuestTaskAdvance> Observe(
        Player player,
        QuestObjectiveKind objective,
        DefinitionId? targetDefinitionId = null,
        int amount = 1)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        if (targetDefinitionId is { } target && target.IsEmpty)
            throw new ArgumentException("TargetDefinitionId vacío.", nameof(targetDefinitionId));

        var changes = new List<QuestTaskAdvance>();
        foreach (var progress in player.Quests.Entries.Where(static entry => entry.State == QuestRuntimeState.Active).ToArray())
        {
            if (!definitions.TryGet<QuestDefinition>(progress.QuestId, out var quest) || quest is null || !quest.Enabled)
                continue;

            foreach (var task in quest.Tasks)
            {
                if (task.Objective != objective || progress.ProgressOf(task.Id) >= task.Quantity) continue;
                if (task.TargetDefinitionId is { } required && targetDefinitionId != required) continue;

                var previous = progress.ProgressOf(task.Id);
                var current = progress.Advance(task, amount);
                if (current != previous)
                    changes.Add(new QuestTaskAdvance(progress.QuestId, task.Id, previous, current, task.Quantity));
            }
        }

        if (changes.Count > 0) player.MarkDirty();
        return changes;
    }

    public QuestProgress Complete(Player player, DefinitionId questId, ConditionEvaluationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        var definition = RequireQuest(questId);
        var progress = RequireActive(player, questId);

        foreach (var task in definition.Tasks)
        {
            if (progress.ProgressOf(task.Id) < task.Quantity)
                throw new InvalidOperationException($"La tarea '{task.Description}' aún no está completa.");
            if (!conditions.Evaluate(player, task.CompletionConditions, context))
                throw new InvalidOperationException($"No se cumplen las condiciones de cierre de la tarea '{task.Description}'.");
        }

        progress.State = QuestRuntimeState.Completed;
        progress.CompletionCount = checked(progress.CompletionCount + 1);
        player.MarkDirty();
        return progress;
    }

    public QuestProgress Fail(Player player, DefinitionId questId)
    {
        ArgumentNullException.ThrowIfNull(player);
        RequireQuest(questId);
        var progress = RequireActive(player, questId);
        progress.State = QuestRuntimeState.Failed;
        player.MarkDirty();
        return progress;
    }

    private QuestDefinition RequireQuest(DefinitionId questId)
    {
        if (questId.IsEmpty) throw new ArgumentException("QuestId vacío.", nameof(questId));
        if (!definitions.TryGet<QuestDefinition>(questId, out var definition) || definition is null)
            throw new KeyNotFoundException("QuestDefinition inexistente.");
        return definition;
    }

    private static QuestProgress RequireActive(Player player, DefinitionId questId)
    {
        if (!player.Quests.TryGet(questId, out var progress) || progress is null || progress.State != QuestRuntimeState.Active)
            throw new InvalidOperationException("La misión no está activa.");
        return progress;
    }
}
