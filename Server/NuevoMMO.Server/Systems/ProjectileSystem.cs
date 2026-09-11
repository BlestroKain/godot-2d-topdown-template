using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public readonly record struct ProjectileStepResult(bool Moved, bool Expired, bool HitWorldBoundary);

/// <summary>
/// Movimiento y validación espacial de proyectiles. No decide daño ni efectos: al registrar un impacto
/// el ejecutor de técnicas puede aplicar las acciones correspondientes sin acoplar proyectil y combate.
/// Usa la misma geometría continua de mapa que movimiento, respetando BlocksProjectiles por separado.
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
        var collider = projectile.CollisionProfile.MovementCollider;
        var desiredInside = collider is null
            ? map.Bounds.Contains(desired)
            : CollisionGeometry.IsInside(collider, desired, map.Bounds);
        var runtime = MapCollisionRuntime.For(map);
        var motion = MotionSolver2D.Resolve(
            before,
            desired - before,
            map.Bounds,
            collider,
            position => runtime.BlocksProjectile(collider, position));

        if (motion.Constrained)
        {
            projectile.MoveTo(motion.Final, Vector2Data.Zero);
            projectile.MarkExpired();
        }

        return new(projectile.Position != before, projectile.IsExpired, !desiredInside);
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
