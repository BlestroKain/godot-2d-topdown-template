using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

/// <summary>
/// Estado runtime de un proyectil. La técnica/acción decide qué ocurre al impactar; esta entidad
/// solo conserva procedencia, trayectoria, vida útil, distancia y objetivos ya impactados.
/// </summary>
public sealed class Projectile : Entity
{
    private readonly HashSet<EntityId> impacted = [];

    public Projectile(EntityId id, DefinitionId? definition, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
        => DefinitionId = definition;

    public Projectile(
        EntityId id,
        DefinitionId? definition,
        EntityId source,
        DefinitionId? technique,
        MapInstanceId mapInstance,
        Vector2Data position,
        Vector2Data direction,
        float speed,
        float maxDistance,
        int lifetimeMilliseconds,
        int maxImpacts,
        ContentKey visualKey,
        string displayName,
        bool canImpactSource = false)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (source.Value <= 0) throw new ArgumentException("Source inválido.", nameof(source));
        if (definition is { } definitionId && definitionId.IsEmpty)
            throw new ArgumentException("DefinitionId vacío.", nameof(definition));
        if (technique is { } techniqueId && techniqueId.IsEmpty)
            throw new ArgumentException("TechniqueId vacío.", nameof(technique));
        if (!direction.IsFinite || direction.IsZero) throw new ArgumentException("Dirección inválida.", nameof(direction));
        if (!float.IsFinite(speed) || speed <= 0) throw new ArgumentOutOfRangeException(nameof(speed));
        if (!float.IsFinite(maxDistance) || maxDistance < 0) throw new ArgumentOutOfRangeException(nameof(maxDistance));
        if (lifetimeMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(lifetimeMilliseconds));
        if (maxImpacts < 1) throw new ArgumentOutOfRangeException(nameof(maxImpacts));

        DefinitionId = definition;
        SourceId = source;
        TechniqueId = technique;
        Origin = position;
        Speed = speed;
        MaxDistance = maxDistance;
        LifetimeMilliseconds = lifetimeMilliseconds;
        RemainingLifetimeMilliseconds = lifetimeMilliseconds;
        MaxImpacts = maxImpacts;
        CanImpactSource = canImpactSource;
        MoveTo(position, direction.Normalized() * speed);
    }

    public DefinitionId? DefinitionId { get; }
    public EntityId? SourceId { get; }
    public DefinitionId? TechniqueId { get; }
    public Vector2Data Origin { get; }
    public float Speed { get; }
    public float MaxDistance { get; }
    public float TraveledDistance { get; private set; }
    public int LifetimeMilliseconds { get; }
    public int RemainingLifetimeMilliseconds { get; private set; }
    public int MaxImpacts { get; } = 1;
    public bool CanImpactSource { get; }
    public int ImpactCount => impacted.Count;
    public IReadOnlyCollection<EntityId> ImpactedEntities => impacted;
    public bool Expired { get; private set; }
    public bool IsExpired => Expired || ImpactCount >= MaxImpacts ||
        (LifetimeMilliseconds > 0 && RemainingLifetimeMilliseconds <= 0) ||
        (MaxDistance > 0 && TraveledDistance >= MaxDistance);

    public bool CanImpact(Entity target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (IsExpired || target.Id == Id || target.MapInstanceId != MapInstanceId || impacted.Contains(target.Id)) return false;
        return CanImpactSource || SourceId is null || target.Id != SourceId.Value;
    }

    public bool RegisterImpact(Entity target)
    {
        if (!CanImpact(target)) return false;
        impacted.Add(target.Id);
        if (ImpactCount >= MaxImpacts) Stop();
        return true;
    }

    public void Advance(int deltaMilliseconds)
    {
        if (deltaMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaMilliseconds));
        if (deltaMilliseconds == 0 || IsExpired) return;

        var seconds = deltaMilliseconds / 1000f;
        var displacement = Velocity * seconds;
        if (MaxDistance > 0)
        {
            var remainingDistance = Math.Max(0f, MaxDistance - TraveledDistance);
            displacement = displacement.ClampLength(remainingDistance);
        }

        var next = Position + displacement;
        TraveledDistance += displacement.Length;
        if (LifetimeMilliseconds > 0)
            RemainingLifetimeMilliseconds = Math.Max(0, RemainingLifetimeMilliseconds - deltaMilliseconds);

        MoveTo(next, IsExpired ? Vector2Data.Zero : Velocity);
        if (IsExpired) Stop();
    }

    public void MarkExpired()
    {
        Expired = true;
        Stop();
    }

    public override EntityState ToState() => new ProjectileState(Id, DefinitionId, MapInstanceId, Position, Velocity,
        Direction, VisualKey, DisplayName);

    private void Stop() => MoveTo(Position, Vector2Data.Zero);
}
