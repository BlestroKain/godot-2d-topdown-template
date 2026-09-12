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
}
