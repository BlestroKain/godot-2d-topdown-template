using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public readonly record struct ProjectileStepResult(bool Moved, bool Expired, bool HitWorldBoundary);

/// <summary>
/// Movimiento y validación espacial de proyectiles. No decide daño ni efectos: al registrar un impacto
/// el ejecutor de técnicas puede aplicar las acciones correspondientes sin acoplar proyectil y combate.
/// </summary>
public sealed class ProjectileSystem
{
    public ProjectileStepResult Step(Projectile projectile, MapDefinition map, int deltaMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(projectile);
        ArgumentNullException.ThrowIfNull(map);
        if (deltaMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaMilliseconds));
        if (projectile.IsExpired) return new(false, true, false);

        var before = projectile.Position;
        projectile.Advance(deltaMilliseconds);
        var desired = projectile.Position;
        var clamped = map.Bounds.Clamp(desired);
        var hitBoundary = clamped != desired;

        if (hitBoundary || MovementSystem.IsBlocked(map, clamped))
        {
            projectile.MoveTo(clamped, Vector2Data.Zero);
            projectile.MarkExpired();
        }

        return new(projectile.Position != before, projectile.IsExpired, hitBoundary);
    }

    public bool IsWithinImpactRadius(Projectile projectile, Entity target, float radius)
    {
        ArgumentNullException.ThrowIfNull(projectile);
        ArgumentNullException.ThrowIfNull(target);
        if (!float.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (!projectile.CanImpact(target)) return false;
        return projectile.Position.DistanceSquaredTo(target.Position) <= radius * radius;
    }

    public bool TryRegisterImpact(Projectile projectile, Entity target, float radius)
        => IsWithinImpactRadius(projectile, target, radius) && projectile.RegisterImpact(target);
}
