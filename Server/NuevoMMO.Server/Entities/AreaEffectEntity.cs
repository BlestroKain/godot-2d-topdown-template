using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

/// <summary>
/// Zona persistente creada por una técnica. El runtime aplica ticks; la definición decide daño/efecto.
/// </summary>
public sealed class AreaEffectEntity : Entity
{
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
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (source.Value <= 0) throw new ArgumentException("Source inválido.", nameof(source));
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (lifetimeMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(lifetimeMilliseconds));
        if (tickIntervalMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(tickIntervalMilliseconds));
        ArgumentNullException.ThrowIfNull(action);

        SourceId = source;
        TechniqueId = technique;
        Action = action;
        Radius = radius;
        RemainingMilliseconds = lifetimeMilliseconds;
        TickIntervalMilliseconds = tickIntervalMilliseconds;
        NextTickMilliseconds = 0;
    }

    public EntityId SourceId { get; }
    public DefinitionId? TechniqueId { get; }
    public TechniqueActionDefinition Action { get; }
    public float Radius { get; }
    public int RemainingMilliseconds { get; private set; }
    public int TickIntervalMilliseconds { get; }
    public int NextTickMilliseconds { get; private set; }
    public bool IsExpired => RemainingMilliseconds <= 0;

    public bool Advance(int deltaMilliseconds)
    {
        if (deltaMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaMilliseconds));
        if (IsExpired) return false;
        RemainingMilliseconds = Math.Max(0, RemainingMilliseconds - deltaMilliseconds);
        if (TickIntervalMilliseconds <= 0) return RemainingMilliseconds <= 0;
        NextTickMilliseconds -= deltaMilliseconds;
        return NextTickMilliseconds <= 0;
    }

    public void MarkTickScheduled()
        => NextTickMilliseconds = TickIntervalMilliseconds;

    public bool Contains(Vector2Data point)
        => Position.DistanceSquaredTo(point) <= Radius * Radius;

    public override EntityState ToState()
        => new ProjectileState(Id, TechniqueId, MapInstanceId, Position, Velocity, Direction, VisualKey, DisplayName);
}
