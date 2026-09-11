using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

/// <summary>Consultas geométricas puras entre roles semánticos; no aplica daño ni reglas de facción.</summary>
public static class SemanticCollisionService
{
    private static readonly CircleCollisionShape PointProbe = new(.001f);

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
        if (interaction is null) return IsWithinRange(actor.Position, target, fallbackRange);
        return ShapeTouchesTarget(interaction, actor.Position, target);
    }

    public static bool AnyHitboxTouchesAnyHurtbox(Entity source, Entity target)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(target);
        foreach (var hitbox in source.CollisionProfile.Hitboxes)
            if (ShapeTouchesTarget(hitbox, source.Position, target)) return true;
        return false;
    }

    /// <summary>
    /// Comprueba una geometría ofensiva/área contra las Hurtboxes del objetivo. Durante la migración,
    /// entidades sin Hurtbox explícita usan MovementCollider; si tampoco existe se usa un punto mínimo.
    /// El sprite nunca participa en la consulta.
    /// </summary>
    public static bool ShapeTouchesTarget(CollisionShape shape, Vector2Data origin, Entity target)
    {
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentNullException.ThrowIfNull(target);
        if (!origin.IsFinite) throw new ArgumentException("Origen no finito.", nameof(origin));
        foreach (var targetShape in CombatTargetShapes(target))
            if (CollisionGeometry.Overlaps(shape, origin, targetShape, target.Position)) return true;
        return false;
    }

    public static bool IsWithinRange(Vector2Data origin, Entity target, float range)
    {
        if (!origin.IsFinite) throw new ArgumentException("Origen no finito.", nameof(origin));
        ArgumentNullException.ThrowIfNull(target);
        if (!float.IsFinite(range) || range < 0) throw new ArgumentOutOfRangeException(nameof(range));
        if (range == 0) return target.Position == origin;
        return ShapeTouchesTarget(new CircleCollisionShape(range), origin, target);
    }

    public static IReadOnlyList<CollisionShape> CombatTargetShapes(Entity target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.CollisionProfile.Hurtboxes.Length > 0) return target.CollisionProfile.Hurtboxes;
        if (target.CollisionProfile.MovementCollider is { } movement) return [movement];
        return [PointProbe];
    }

    public static float NavigationClearance(Entity entity)
        => (entity ?? throw new ArgumentNullException(nameof(entity))).CollisionProfile.NavigationRadius;
}
