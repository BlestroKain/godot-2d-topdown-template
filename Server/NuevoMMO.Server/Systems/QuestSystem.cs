using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

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
