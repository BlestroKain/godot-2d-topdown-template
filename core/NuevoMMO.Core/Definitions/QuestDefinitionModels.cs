namespace NuevoMMO.Core;

public enum QuestObjectiveKind : byte
{
    EventDriven,
    GatherItems,
    KillMobs,
    ExploreArea,
    Interact,
    CraftItem,
    UseItem,
    ReachProfessionMastery,
    Custom
}

public sealed record QuestTaskDefinition
{
    public QuestTaskDefinition(
        Guid id,
        string description,
        QuestObjectiveKind objective = QuestObjectiveKind.EventDriven,
        DefinitionId? targetDefinitionId = null,
        int quantity = 1,
        DefinitionId? completionEventId = null,
        ConditionGroupDefinition? completionConditions = null,
        Dictionary<string, float>? parameters = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("QuestTask ID vacío.", nameof(id));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description requerida.", nameof(description));
        if (targetDefinitionId is { } target && target.IsEmpty) throw new ArgumentException("TargetDefinitionId vacío.", nameof(targetDefinitionId));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (completionEventId is { } evt && evt.IsEmpty) throw new ArgumentException("CompletionEventId vacío.", nameof(completionEventId));

        Id = id;
        Description = description.Trim();
        Objective = objective;
        TargetDefinitionId = targetDefinitionId;
        Quantity = quantity;
        CompletionEventId = completionEventId;
        CompletionConditions = completionConditions ?? ConditionGroupDefinition.Empty;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Guid Id { get; }
    public string Description { get; }
    public QuestObjectiveKind Objective { get; }
    public DefinitionId? TargetDefinitionId { get; }
    public int Quantity { get; }
    public DefinitionId? CompletionEventId { get; }
    public ConditionGroupDefinition CompletionConditions { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record QuestLogPresentationDefinition(
    string UnstartedCategory = "",
    string InProgressCategory = "",
    string CompletedCategory = "",
    int Order = 0,
    bool LogBeforeOffer = false,
    bool LogAfterComplete = false,
    bool HideUnlessRequirementsMet = false);
