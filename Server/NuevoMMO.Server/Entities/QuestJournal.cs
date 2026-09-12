using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public enum QuestRuntimeState : byte
{
    NotStarted,
    Active,
    Completed,
    Failed
}

public sealed class QuestProgress
{
    private readonly Dictionary<Guid, int> taskProgress = [];

    internal QuestProgress(DefinitionId questId)
    {
        if (questId.IsEmpty) throw new ArgumentException("QuestId vacío.", nameof(questId));
        QuestId = questId;
    }

    public DefinitionId QuestId { get; }
    public QuestRuntimeState State { get; internal set; } = QuestRuntimeState.NotStarted;
    public int CompletionCount { get; internal set; }
    public IReadOnlyDictionary<Guid, int> TaskProgress => taskProgress;

    public int ProgressOf(Guid taskId) => taskProgress.GetValueOrDefault(taskId);

    internal void ResetForStart(IEnumerable<QuestTaskDefinition> tasks)
    {
        taskProgress.Clear();
        foreach (var task in tasks) taskProgress[task.Id] = 0;
        State = QuestRuntimeState.Active;
    }

    internal int Advance(QuestTaskDefinition task, int amount)
    {
        if (State != QuestRuntimeState.Active) throw new InvalidOperationException("La misión no está activa.");
        if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        var current = taskProgress.GetValueOrDefault(task.Id);
        var next = Math.Min(task.Quantity, checked(current + amount));
        taskProgress[task.Id] = next;
        return next;
    }

    internal void Restore(QuestRuntimeState state, int completionCount, IEnumerable<KeyValuePair<Guid, int>> tasks)
    {
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        if (completionCount < 0) throw new ArgumentOutOfRangeException(nameof(completionCount));
        taskProgress.Clear();
        foreach (var pair in tasks)
        {
            if (pair.Key == Guid.Empty || pair.Value < 0) continue;
            taskProgress[pair.Key] = pair.Value;
        }
        State = state;
        CompletionCount = completionCount;
    }
}

public sealed class QuestJournal
{
    private readonly Dictionary<DefinitionId, QuestProgress> progress = [];

    public IReadOnlyCollection<QuestProgress> Entries => progress.Values;

    public QuestRuntimeState StateOf(DefinitionId questId)
        => progress.TryGetValue(questId, out var entry) ? entry.State : QuestRuntimeState.NotStarted;

    public bool TryGet(DefinitionId questId, out QuestProgress? entry)
        => progress.TryGetValue(questId, out entry);

    internal QuestProgress Begin(QuestDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!progress.TryGetValue(definition.Id, out var entry))
        {
            entry = new QuestProgress(definition.Id);
            progress.Add(definition.Id, entry);
        }
        entry.ResetForStart(definition.Tasks);
        return entry;
    }

    internal void Restore(
        DefinitionId questId,
        QuestRuntimeState state,
        int completionCount,
        IEnumerable<KeyValuePair<Guid, int>> tasks)
    {
        if (questId.IsEmpty) return;
        if (!progress.TryGetValue(questId, out var entry))
        {
            entry = new QuestProgress(questId);
            progress.Add(questId, entry);
        }
        entry.Restore(state, completionCount, tasks);
    }

    internal void Clear() => progress.Clear();
}
