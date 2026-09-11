using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

/// <summary>Consultas geométricas puras entre roles semánticos; no aplica daño ni reglas de facción.</summary>
public static class SemanticCollisionService
{
    public static bool MovementBodiesOverlap(Entity mover, Vector2Data prospectivePosition, Entity obstacle)
    {
        ArgumentNullException.ThrowIfNull(mover); ArgumentNullException.ThrowIfNull(obstacle);
        if (!obstacle.CollisionProfile.BlocksMovement) return false;
        var a = mover.CollisionProfile.MovementCollider;
        var b = obstacle.CollisionProfile.MovementCollider;
        return a is not null && b is not null && CollisionGeometry.Overlaps(a, prospectivePosition, b, obstacle.Position);
    }

    public static bool IsWithinInteractionReach(Entity actor, Entity target, float fallbackRange)
    {
        ArgumentNullException.ThrowIfNull(actor); ArgumentNullException.ThrowIfNull(target);
        if (!float.IsFinite(fallbackRange) || fallbackRange < 0) throw new ArgumentOutOfRangeException(nameof(fallbackRange));
        var interaction = actor.CollisionProfile.InteractionShape;
        if (interaction is null) return actor.Position.DistanceSquaredTo(target.Position) <= fallbackRange * fallbackRange;
        var targetShapes = target.CollisionProfile.Hurtboxes.Length > 0
            ? target.CollisionProfile.Hurtboxes
            : target.CollisionProfile.MovementCollider is { } movement ? [movement] : [];
        return targetShapes.Length == 0
            ? CollisionGeometry.Overlaps(interaction, actor.Position, new CircleCollisionShape(.001f), target.Position)
            : targetShapes.Any(shape => CollisionGeometry.Overlaps(interaction, actor.Position, shape, target.Position));
    }

    public static bool AnyHitboxTouchesAnyHurtbox(Entity source, Entity target)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(target);
        foreach (var hitbox in source.CollisionProfile.Hitboxes)
            foreach (var hurtbox in target.CollisionProfile.Hurtboxes)
                if (CollisionGeometry.Overlaps(hitbox, source.Position, hurtbox, target.Position)) return true;
        return false;
    }

    public static float NavigationClearance(Entity entity)
        => (entity ?? throw new ArgumentNullException(nameof(entity))).CollisionProfile.NavigationRadius;
}
