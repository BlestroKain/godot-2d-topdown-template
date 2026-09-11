using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

/// <summary>
/// Zona persistente creada por una técnica. La misma entidad sirve para campos periódicos,
/// glifos OnEnter y trampas/efectos OnExit; la definición decide cuándo dispara su payload.
/// </summary>
public sealed class AreaEffectEntity : Entity
{
    private readonly HashSet<EntityId> occupants = [];
    private readonly HashSet<EntityId> activatedTargets = [];
    private int activationCount;
    private bool exhausted;

    /// <summary>Compatibilidad con contenido temprano: construye una zona periódica.</summary>
    public AreaEffectEntity(
        EntityId id,
        EntityId source,
        DefinitionId? technique,
        TechniqueActionDefinition action,
        MapInstanceId mapInstance,
        Vector2Data position,
        float radius,
        int lifetimeMilliseconds,
        int tickIntervalMilliseconds,
        ContentKey visualKey,
        string displayName)
        : this(
            id,
            source,
            technique,
            action,
            new TechniqueZoneDefinition(radius, lifetimeMilliseconds, Math.Max(1, tickIntervalMilliseconds)),
            mapInstance,
            position,
            visualKey,
            displayName)
    {
    }

    public AreaEffectEntity(
        EntityId id,
        EntityId source,
        DefinitionId? technique,
        TechniqueActionDefinition action,
        TechniqueZoneDefinition zone,
        MapInstanceId mapInstance,
        Vector2Data position,
        ContentKey visualKey,
        string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (source.Value <= 0) throw new ArgumentException("Source inválido.", nameof(source));
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(zone);

        SourceId = source;
        TechniqueId = technique;
        Action = action;
        Zone = zone;
        RemainingMilliseconds = zone.LifetimeMilliseconds;
        NextTickMilliseconds = zone.TriggerMode == TechniqueZoneTriggerMode.Periodic ? 0 : int.MaxValue;
    }

    public EntityId SourceId { get; }
    public DefinitionId? TechniqueId { get; }
    public TechniqueActionDefinition Action { get; }
    public TechniqueZoneDefinition Zone { get; }
    public float Radius => Zone.Radius;
    public int RemainingMilliseconds { get; private set; }
    public int TickIntervalMilliseconds => Zone.TickIntervalMilliseconds;
    public int NextTickMilliseconds { get; private set; }
    public int ActivationCount => activationCount;
    public bool IsExpired => RemainingMilliseconds <= 0 || exhausted;

    /// <summary>
    /// Avanza vida/temporizador. Devuelve true únicamente cuando una zona Periodic debe pulsar.
    /// OnEnter/OnExit se resuelven contra ocupación en WorldRuntime.
    /// </summary>
    public bool Advance(int deltaMilliseconds)
    {
        if (deltaMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaMilliseconds));
        if (IsExpired) return false;

        RemainingMilliseconds = Math.Max(0, RemainingMilliseconds - deltaMilliseconds);
        if (Zone.TriggerMode != TechniqueZoneTriggerMode.Periodic || RemainingMilliseconds <= 0) return false;

        NextTickMilliseconds -= deltaMilliseconds;
        return NextTickMilliseconds <= 0;
    }

    public void MarkTickScheduled()
    {
        if (Zone.TriggerMode == TechniqueZoneTriggerMode.Periodic)
            NextTickMilliseconds = Zone.TickIntervalMilliseconds;
    }

    public bool Contains(Vector2Data point)
        => Position.DistanceSquaredTo(point) <= Radius * Radius;

    public bool WasInside(EntityId target) => occupants.Contains(target);

    public void SetInside(EntityId target, bool inside)
    {
        if (inside) occupants.Add(target);
        else occupants.Remove(target);
    }

    /// <summary>
    /// Aplica límites globales/per-target de activación. Devuelve false si el payload no debe dispararse.
    /// </summary>
    public bool TryRegisterActivation(EntityId target)
    {
        if (target.Value <= 0 || IsExpired) return false;
        if (Zone.OncePerTarget && activatedTargets.Contains(target)) return false;
        if (Zone.MaxActivations > 0 && activationCount >= Zone.MaxActivations)
        {
            exhausted = true;
            return false;
        }

        activationCount++;
        activatedTargets.Add(target);
        if (Zone.MaxActivations > 0 && activationCount >= Zone.MaxActivations)
            exhausted = true;
        return true;
    }

    public override EntityState ToState()
        => new ProjectileState(Id, TechniqueId, MapInstanceId, Position, Velocity, Direction, VisualKey, DisplayName);
}
