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

/// <summary>
/// Comportamiento físico/lógico de un proyectil creado por una acción. CollisionRadius controla
/// colisión con el mapa; HitRadius controla impacto contra Hurtboxes y puede ser mayor o menor.
/// </summary>
public sealed record TechniqueProjectileDefinition
{
    public TechniqueProjectileDefinition(
        float speed = 220f,
        float maxDistance = 320f,
        int lifetimeMilliseconds = 2500,
        int maxImpacts = 1,
        float collisionRadius = 0f,
        float hitRadius = 16f,
        bool canImpactSource = false)
    {
        if (!float.IsFinite(speed) || speed <= 0) throw new ArgumentOutOfRangeException(nameof(speed));
        if (!float.IsFinite(maxDistance) || maxDistance < 0) throw new ArgumentOutOfRangeException(nameof(maxDistance));
        if (lifetimeMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(lifetimeMilliseconds));
        if (maxImpacts < 1) throw new ArgumentOutOfRangeException(nameof(maxImpacts));
        if (!float.IsFinite(collisionRadius) || collisionRadius < 0) throw new ArgumentOutOfRangeException(nameof(collisionRadius));
        if (!float.IsFinite(hitRadius) || hitRadius <= 0) throw new ArgumentOutOfRangeException(nameof(hitRadius));
        Speed = speed;
        MaxDistance = maxDistance;
        LifetimeMilliseconds = lifetimeMilliseconds;
        MaxImpacts = maxImpacts;
        CollisionRadius = collisionRadius;
        HitRadius = hitRadius;
        CanImpactSource = canImpactSource;
    }

    public float Speed { get; }
    public float MaxDistance { get; }
    public int LifetimeMilliseconds { get; }
    public int MaxImpacts { get; }
    public float CollisionRadius { get; }
    public float HitRadius { get; }
    public bool CanImpactSource { get; }
}

/// <summary>
/// Cuándo una zona persistente dispara su payload. El AoE instantáneo no necesita SpawnZone:
/// se expresa con TargetMode.Area + acciones Impact. Periodic cubre campos persistentes;
/// OnEnter/OnExit son la base común para glifos y trampas.
/// </summary>
public enum TechniqueZoneTriggerMode : byte
{
    Periodic,
    OnEnter,
    OnExit
}

public sealed record TechniqueZoneDefinition
{
    public TechniqueZoneDefinition(
        float radius = 48f,
        int lifetimeMilliseconds = 2000,
        int tickIntervalMilliseconds = 500,
        TechniqueZoneTriggerMode triggerMode = TechniqueZoneTriggerMode.Periodic,
        int maxActivations = 0,
        bool oncePerTarget = false,
        bool includeSource = false)
    {
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (lifetimeMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(lifetimeMilliseconds));
        if (tickIntervalMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(tickIntervalMilliseconds));
        if (triggerMode == TechniqueZoneTriggerMode.Periodic && tickIntervalMilliseconds < 1)
            throw new ArgumentException("Una zona Periodic requiere TickIntervalMilliseconds mayor que cero.", nameof(tickIntervalMilliseconds));
        if (maxActivations < 0) throw new ArgumentOutOfRangeException(nameof(maxActivations));
        Radius = radius;
        LifetimeMilliseconds = lifetimeMilliseconds;
        TickIntervalMilliseconds = tickIntervalMilliseconds;
        TriggerMode = triggerMode;
        MaxActivations = maxActivations;
        OncePerTarget = oncePerTarget;
        IncludeSource = includeSource;
    }

    public float Radius { get; }
    public int LifetimeMilliseconds { get; }
    public int TickIntervalMilliseconds { get; }
    public TechniqueZoneTriggerMode TriggerMode { get; }

    /// <summary>0 = sin límite global de activaciones durante la vida de la zona.</summary>
    public int MaxActivations { get; }
    public bool OncePerTarget { get; }
    public bool IncludeSource { get; }
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
        Dictionary<string, string>? metadata = null,
        TechniqueActionDefinition[]? payloadActions = null,
        TechniqueProjectileDefinition? projectile = null,
        TechniqueZoneDefinition? zone = null)
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
        if (projectile is not null && kind != TechniqueActionKind.SpawnProjectile)
            throw new ArgumentException("Projectile solo es válido para SpawnProjectile.", nameof(projectile));
        if (zone is not null && kind != TechniqueActionKind.SpawnZone)
            throw new ArgumentException("Zone solo es válido para SpawnZone.", nameof(zone));

        var payload = payloadActions?.ToArray() ?? [];
        if (payload.Any(static action => action is null))
            throw new ArgumentException("PayloadActions no puede contener acciones nulas.", nameof(payloadActions));
        if (payload.Length > 64)
            throw new ArgumentException("PayloadActions excede el máximo de 64 acciones.", nameof(payloadActions));

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
        PayloadActions = payload;
        Projectile = projectile;
        Zone = zone;
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

    /// <summary>
    /// Acciones que pertenecen a la entidad/efecto creado por esta acción. Un proyectil las ejecuta
    /// al impactar; una zona las ejecuta según su TriggerMode. Permite combinar daño, efectos,
    /// desplazamiento y otras mecánicas sin hardcodear comportamiento en Projectile/WorldRuntime.
    /// </summary>
    public TechniqueActionDefinition[] PayloadActions { get; }
    public TechniqueProjectileDefinition? Projectile { get; }
    public TechniqueZoneDefinition? Zone { get; }
}
