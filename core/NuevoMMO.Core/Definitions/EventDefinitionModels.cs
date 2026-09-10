namespace NuevoMMO.Core;

public enum ConditionGroupMode : byte
{
    All,
    Any,
    None
}

public enum ConditionKind : byte
{
    Always,
    Variable,
    Switch,
    HasItem,
    ItemEquipped,
    HasTechnique,
    HasTradition,
    QuestState,
    ProfessionMastery,
    CharacterLevel,
    Stat,
    Map,
    Region,
    Time,
    RandomChance,
    EntityState,
    AccountFlag,
    CharacterFlag,
    Custom
}

public enum ComparisonOperator : byte
{
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual,
    Contains,
    Exists
}

public sealed record ConditionDefinition
{
    public ConditionDefinition(
        ConditionKind kind,
        ComparisonOperator comparison = ComparisonOperator.Equal,
        Dictionary<string, DefinitionId>? references = null,
        Dictionary<string, float>? numbers = null,
        Dictionary<string, string>? text = null,
        bool negate = false)
    {
        Kind = kind;
        Comparison = comparison;
        References = DefinitionModelGuards.CopyDefinitionHooks(references, nameof(references));
        Numbers = DefinitionModelGuards.CopyFinite(numbers, nameof(numbers));
        Text = DefinitionModelGuards.CopyText(text, nameof(text));
        Negate = negate;
    }

    public ConditionKind Kind { get; }
    public ComparisonOperator Comparison { get; }
    public Dictionary<string, DefinitionId> References { get; }
    public Dictionary<string, float> Numbers { get; }
    public Dictionary<string, string> Text { get; }
    public bool Negate { get; }
}

public sealed record ConditionGroupDefinition
{
    public ConditionGroupDefinition(
        ConditionGroupMode mode = ConditionGroupMode.All,
        ConditionDefinition[]? conditions = null,
        ConditionGroupDefinition[]? groups = null)
    {
        Mode = mode;
        Conditions = conditions?.ToArray() ?? [];
        Groups = groups?.ToArray() ?? [];
    }

    public ConditionGroupMode Mode { get; }
    public ConditionDefinition[] Conditions { get; }
    public ConditionGroupDefinition[] Groups { get; }

    public static ConditionGroupDefinition Empty { get; } = new();
}

public enum EventScope : byte
{
    Map,
    Common,
    Global
}

public enum EventTrigger : byte
{
    Action,
    PlayerTouch,
    AreaEnter,
    AreaExit,
    Autorun,
    Parallel,
    ServerStart,
    Timer,
    MapEnter,
    MapLeave,
    EntityDeath,
    ItemUse,
    TechniqueUse,
    QuestChanged,
    VariableChanged,
    Custom
}

public enum EventRenderLayer : byte
{
    BelowEntities,
    SameAsEntities,
    AboveEntities
}

public enum EventMovementMode : byte
{
    Stationary,
    Random,
    Route
}

public enum EventCommandKind : byte
{
    Dialogue,
    Choice,
    Conditional,
    Wait,
    SetVariable,
    SetSwitch,
    GiveItem,
    TakeItem,
    GiveExperience,
    Teleport,
    MoveEntity,
    SpawnEntity,
    DespawnEntity,
    ApplyEffect,
    RemoveEffect,
    LearnTechnique,
    ForgetTechnique,
    StartQuest,
    AdvanceQuest,
    CompleteQuest,
    FailQuest,
    ChangeTradition,
    ModifyProfession,
    PlayAnimation,
    PlaySound,
    ShowNotification,
    TriggerEvent,
    OpenShop,
    OpenBank,
    SetCheckpoint,
    Custom
}

public sealed record EventCommandDefinition
{
    public EventCommandDefinition(
        Guid id,
        EventCommandKind kind,
        Dictionary<string, DefinitionId>? references = null,
        Dictionary<string, float>? numbers = null,
        Dictionary<string, string>? text = null,
        Dictionary<string, Guid>? branches = null,
        ConditionGroupDefinition? condition = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("EventCommand ID vacío.", nameof(id));

        Id = id;
        Kind = kind;
        References = DefinitionModelGuards.CopyDefinitionHooks(references, nameof(references));
        Numbers = DefinitionModelGuards.CopyFinite(numbers, nameof(numbers));
        Text = DefinitionModelGuards.CopyText(text, nameof(text));
        Branches = DefinitionModelGuards.CopyGuidMap(branches, nameof(branches));
        Condition = condition;
    }

    public Guid Id { get; }
    public EventCommandKind Kind { get; }
    public Dictionary<string, DefinitionId> References { get; }
    public Dictionary<string, float> Numbers { get; }
    public Dictionary<string, string> Text { get; }

    /// <summary>
    /// Mapea nombres de rama (true/false, opción, success/failure, etc.) al ID de otra lista de comandos.
    /// </summary>
    public Dictionary<string, Guid> Branches { get; }
    public ConditionGroupDefinition? Condition { get; }
}

public sealed record EventPageVisualDefinition
{
    public EventPageVisualDefinition(
        ContentKey? visualKey = null,
        ContentKey? faceKey = null,
        EventRenderLayer layer = EventRenderLayer.SameAsEntities,
        bool hideName = false,
        bool directionFixed = false,
        bool walkingAnimation = true,
        bool passable = false,
        Dictionary<string, ContentKey>? overrides = null)
    {
        if (visualKey is { } visual && visual.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));
        if (faceKey is { } face && face.IsEmpty) throw new ArgumentException("FaceKey vacío.", nameof(faceKey));

        VisualKey = visualKey;
        FaceKey = faceKey;
        Layer = layer;
        HideName = hideName;
        DirectionFixed = directionFixed;
        WalkingAnimation = walkingAnimation;
        Passable = passable;
        Overrides = DefinitionModelGuards.CopyContentKeys(overrides, nameof(overrides));
    }

    public ContentKey? VisualKey { get; }
    public ContentKey? FaceKey { get; }
    public EventRenderLayer Layer { get; }
    public bool HideName { get; }
    public bool DirectionFixed { get; }
    public bool WalkingAnimation { get; }
    public bool Passable { get; }
    public Dictionary<string, ContentKey> Overrides { get; }
}

public sealed record EventPageMovementDefinition
{
    public EventPageMovementDefinition(
        EventMovementMode mode = EventMovementMode.Stationary,
        float speed = 0,
        Vector2Data[]? route = null,
        bool loopRoute = true,
        bool ignoreNavigationAvoidance = false)
    {
        if (!float.IsFinite(speed) || speed < 0) throw new ArgumentOutOfRangeException(nameof(speed));
        var points = route?.ToArray() ?? [];
        if (points.Any(static point => !point.IsFinite)) throw new ArgumentException("La ruta contiene posiciones no finitas.", nameof(route));
        if (mode == EventMovementMode.Route && points.Length == 0)
            throw new ArgumentException("Un evento con movimiento Route requiere puntos.", nameof(route));

        Mode = mode;
        Speed = speed;
        Route = points;
        LoopRoute = loopRoute;
        IgnoreNavigationAvoidance = ignoreNavigationAvoidance;
    }

    public EventMovementMode Mode { get; }
    public float Speed { get; }
    public Vector2Data[] Route { get; }
    public bool LoopRoute { get; }
    public bool IgnoreNavigationAvoidance { get; }
}

public sealed record EventPageDefinition
{
    public EventPageDefinition(
        Guid id,
        EventTrigger trigger = EventTrigger.Action,
        int priority = 0,
        ConditionGroupDefinition? conditions = null,
        EventPageVisualDefinition? visual = null,
        EventPageMovementDefinition? movement = null,
        float interactionRadius = 32,
        bool freezeDuringInteraction = false,
        Dictionary<Guid, EventCommandDefinition[]>? commandLists = null,
        Guid? rootCommandListId = null,
        Dictionary<string, float>? triggerParameters = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("EventPage ID vacío.", nameof(id));
        if (!float.IsFinite(interactionRadius) || interactionRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(interactionRadius));

        var lists = commandLists is null
            ? new Dictionary<Guid, EventCommandDefinition[]>()
            : commandLists.ToDictionary(static pair => pair.Key, static pair => pair.Value?.ToArray() ?? []);

        if (lists.Keys.Any(static key => key == Guid.Empty))
            throw new ArgumentException("CommandLists contiene un ID vacío.", nameof(commandLists));

        var root = rootCommandListId ?? (lists.Count == 0 ? Guid.Empty : lists.Keys.First());
        if (root != Guid.Empty && !lists.ContainsKey(root))
            throw new ArgumentException("RootCommandListId no existe en CommandLists.", nameof(rootCommandListId));

        foreach (var command in lists.Values.SelectMany(static commands => commands))
        {
            foreach (var branch in command.Branches.Values)
            {
                if (!lists.ContainsKey(branch))
                    throw new ArgumentException($"El comando {command.Id} referencia una lista de comandos inexistente: {branch}.", nameof(commandLists));
            }
        }

        Id = id;
        Trigger = trigger;
        Priority = priority;
        Conditions = conditions ?? ConditionGroupDefinition.Empty;
        Visual = visual ?? new EventPageVisualDefinition();
        Movement = movement ?? new EventPageMovementDefinition();
        InteractionRadius = interactionRadius;
        FreezeDuringInteraction = freezeDuringInteraction;
        CommandLists = lists;
        RootCommandListId = root;
        TriggerParameters = DefinitionModelGuards.CopyFinite(triggerParameters, nameof(triggerParameters));
    }

    public Guid Id { get; }
    public EventTrigger Trigger { get; }
    public int Priority { get; }
    public ConditionGroupDefinition Conditions { get; }
    public EventPageVisualDefinition Visual { get; }
    public EventPageMovementDefinition Movement { get; }
    public float InteractionRadius { get; }
    public bool FreezeDuringInteraction { get; }
    public Dictionary<Guid, EventCommandDefinition[]> CommandLists { get; }
    public Guid RootCommandListId { get; }
    public Dictionary<string, float> TriggerParameters { get; }
}

public sealed record EventPlacementDefinition
{
    public EventPlacementDefinition(DefinitionId mapId, Vector2Data position, Direction direction = Direction.Down)
    {
        if (mapId.IsEmpty) throw new ArgumentException("MapId vacío.", nameof(mapId));
        if (!position.IsFinite) throw new ArgumentException("Position no finita.", nameof(position));
        MapId = mapId;
        Position = position;
        Direction = direction;
    }

    public DefinitionId MapId { get; }
    public Vector2Data Position { get; }
    public Direction Direction { get; }
}
