namespace NuevoMMO.Core;

public enum TechniqueTargetMode : byte
{
    Self,
    Entity,
    Point,
    Direction,
    Area,
    Cone,
    Line
}

[Flags]
public enum TechniqueTargetRelation : byte
{
    None = 0,
    Self = 1,
    Ally = 2,
    Enemy = 4,
    Neutral = 8,
    Any = Self | Ally | Enemy | Neutral
}

public enum TechniqueActionKind : byte
{
    Damage,
    Heal,
    ApplyEffect,
    RemoveEffect,
    Push,
    Pull,
    Teleport,
    Dash,
    SpawnProjectile,
    SpawnZone,
    Summon,
    ModifyVital,
    ModifyResource,
    TriggerEvent,
    Custom
}

public enum TechniqueActionMoment : byte
{
    CastStart,
    CastComplete,
    Impact,
    Tick,
    Expire
}

public sealed record TechniqueTargetingDefinition
{
    public TechniqueTargetingDefinition(
        TechniqueTargetMode mode = TechniqueTargetMode.Entity,
        TechniqueTargetRelation relations = TechniqueTargetRelation.Enemy,
        float range = 0,
        float radius = 0,
        float angleDegrees = 0,
        int maxTargets = 1,
        bool requiresLineOfSight = true,
        bool allowEmptyPoint = false,
        Dictionary<string, float>? parameters = null)
    {
        if (!float.IsFinite(range) || range < 0) throw new ArgumentOutOfRangeException(nameof(range));
        if (!float.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (!float.IsFinite(angleDegrees) || angleDegrees < 0 || angleDegrees > 360)
            throw new ArgumentOutOfRangeException(nameof(angleDegrees));
        if (maxTargets < 1) throw new ArgumentOutOfRangeException(nameof(maxTargets));
        if (relations == TechniqueTargetRelation.None && mode != TechniqueTargetMode.Point && mode != TechniqueTargetMode.Direction)
            throw new ArgumentException("La técnica requiere al menos una relación de objetivo.", nameof(relations));

        Mode = mode;
        Relations = relations;
        Range = range;
        Radius = radius;
        AngleDegrees = angleDegrees;
        MaxTargets = maxTargets;
        RequiresLineOfSight = requiresLineOfSight;
        AllowEmptyPoint = allowEmptyPoint;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public TechniqueTargetMode Mode { get; }
    public TechniqueTargetRelation Relations { get; }
    public float Range { get; }
    public float Radius { get; }
    public float AngleDegrees { get; }
    public int MaxTargets { get; }
    public bool RequiresLineOfSight { get; }
    public bool AllowEmptyPoint { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record TechniqueTimingDefinition
{
    public TechniqueTimingDefinition(
        int castMilliseconds = 0,
        int cooldownMilliseconds = 0,
        string? cooldownGroup = null,
        bool ignoreGlobalCooldown = false,
        bool ignoreCooldownReduction = false,
        int channelMilliseconds = 0,
        int tickIntervalMilliseconds = 0,
        Dictionary<string, float>? parameters = null)
    {
        if (castMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(castMilliseconds));
        if (cooldownMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(cooldownMilliseconds));
        if (channelMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(channelMilliseconds));
        if (tickIntervalMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(tickIntervalMilliseconds));
        if (channelMilliseconds > 0 && tickIntervalMilliseconds > channelMilliseconds)
            throw new ArgumentException("TickInterval no puede ser mayor que ChannelMilliseconds.", nameof(tickIntervalMilliseconds));

        CastMilliseconds = castMilliseconds;
        CooldownMilliseconds = cooldownMilliseconds;
        CooldownGroup = cooldownGroup?.Trim() ?? string.Empty;
        IgnoreGlobalCooldown = ignoreGlobalCooldown;
        IgnoreCooldownReduction = ignoreCooldownReduction;
        ChannelMilliseconds = channelMilliseconds;
        TickIntervalMilliseconds = tickIntervalMilliseconds;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public int CastMilliseconds { get; }
    public int CooldownMilliseconds { get; }
    public string CooldownGroup { get; }
    public bool IgnoreGlobalCooldown { get; }
    public bool IgnoreCooldownReduction { get; }
    public int ChannelMilliseconds { get; }
    public int TickIntervalMilliseconds { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record TechniqueActionDefinition
{
    public TechniqueActionDefinition(
        TechniqueActionKind kind,
        TechniqueActionMoment moment = TechniqueActionMoment.Impact,
        float amount = 0,
        Element element = Element.Neutral,
        float distance = 0,
        int durationMilliseconds = 0,
        DefinitionId? effectId = null,
        DefinitionId? eventId = null,
        DefinitionId? spawnDefinitionId = null,
        DefinitionId? destinationMapId = null,
        Vector2Data? destination = null,
        Dictionary<StatId, float>? scaling = null,
        Dictionary<string, float>? parameters = null,
        Dictionary<string, string>? metadata = null)
    {
        if (!float.IsFinite(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!float.IsFinite(distance) || distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
        if (durationMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
        if (effectId is { } effect && effect.IsEmpty) throw new ArgumentException("EffectId vacío.", nameof(effectId));
        if (eventId is { } evt && evt.IsEmpty) throw new ArgumentException("EventId vacío.", nameof(eventId));
        if (spawnDefinitionId is { } spawn && spawn.IsEmpty) throw new ArgumentException("SpawnDefinitionId vacío.", nameof(spawnDefinitionId));
        if (destinationMapId is { } map && map.IsEmpty) throw new ArgumentException("DestinationMapId vacío.", nameof(destinationMapId));
        if (destination is { } point && !point.IsFinite) throw new ArgumentException("Destination no finita.", nameof(destination));
        if ((kind is TechniqueActionKind.ApplyEffect or TechniqueActionKind.RemoveEffect) && effectId is null)
            throw new ArgumentException("La acción de efecto requiere EffectId.", nameof(effectId));
        if (kind == TechniqueActionKind.TriggerEvent && eventId is null)
            throw new ArgumentException("TriggerEvent requiere EventId.", nameof(eventId));
        if (kind == TechniqueActionKind.Teleport && destinationMapId is null && destination is null)
            throw new ArgumentException("Teleport requiere DestinationMapId y/o Destination.");

        Kind = kind;
        Moment = moment;
        Amount = amount;
        Element = element;
        Distance = distance;
        DurationMilliseconds = durationMilliseconds;
        EffectId = effectId;
        EventId = eventId;
        SpawnDefinitionId = spawnDefinitionId;
        DestinationMapId = destinationMapId;
        Destination = destination;
        Scaling = DefinitionModelGuards.CopyFinite(scaling, nameof(scaling));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
        Metadata = DefinitionCollectionGuards.CopyText(metadata, nameof(metadata));
    }

    public TechniqueActionKind Kind { get; }
    public TechniqueActionMoment Moment { get; }
    public float Amount { get; }
    public Element Element { get; }
    public float Distance { get; }
    public int DurationMilliseconds { get; }
    public DefinitionId? EffectId { get; }
    public DefinitionId? EventId { get; }
    public DefinitionId? SpawnDefinitionId { get; }
    public DefinitionId? DestinationMapId { get; }
    public Vector2Data? Destination { get; }
    public Dictionary<StatId, float> Scaling { get; }
    public Dictionary<string, float> Parameters { get; }
    public Dictionary<string, string> Metadata { get; }
}
