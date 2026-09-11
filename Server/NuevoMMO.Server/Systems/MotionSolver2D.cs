using NuevoMMO.Core;

namespace NuevoMMO.Server.Systems;

public readonly record struct MotionResolution(
    Vector2Data Start,
    Vector2Data Desired,
    Vector2Data Final,
    bool Constrained,
    bool BlockedX,
    bool BlockedY);

/// <summary>
/// Movimiento continuo autoritativo. Divide recorridos grandes en pasos de máximo una unidad de mundo
/// para evitar tunneling y prueba ejes por separado para deslizar contra límites/obstáculos.
/// </summary>
public static class MotionSolver2D
{
    public const float MaximumSubstep = 1f;

    public static MotionResolution Resolve(
        Vector2Data start,
        Vector2Data delta,
        BoundsData worldBounds,
        CollisionShape? movementCollider = null,
        Func<Vector2Data, bool>? blockedAt = null)
    {
        if (!start.IsFinite || !delta.IsFinite || !worldBounds.IsValid) throw new ArgumentException("Movimiento no finito o bounds inválidos.");
        if (!CanOccupy(start, worldBounds, movementCollider, blockedAt))
            throw new InvalidOperationException("La posición autoritativa inicial está fuera del espacio transitable.");

        var desired = start + delta;
        var distance = delta.Length;
        var steps = Math.Max(1, (int)MathF.Ceiling(distance / MaximumSubstep));
        var increment = delta / steps;
        var current = start;
        var blockedX = false;
        var blockedY = false;

        for (var step = 0; step < steps; step++)
        {
            var full = current + increment;
            if (CanOccupy(full, worldBounds, movementCollider, blockedAt))
            {
                current = full;
                continue;
            }

            var xOnly = new Vector2Data(current.X + increment.X, current.Y);
            var yOnly = new Vector2Data(current.X, current.Y + increment.Y);
            var moved = false;
            if (MathF.Abs(increment.X) > 0 && CanOccupy(xOnly, worldBounds, movementCollider, blockedAt))
            {
                current = xOnly;
                moved = true;
            }
            else if (MathF.Abs(increment.X) > 0) blockedX = true;

            // Y se prueba desde la posición resultante del deslizamiento en X.
            yOnly = new Vector2Data(current.X, current.Y + increment.Y);
            if (MathF.Abs(increment.Y) > 0 && CanOccupy(yOnly, worldBounds, movementCollider, blockedAt))
            {
                current = yOnly;
                moved = true;
            }
            else if (MathF.Abs(increment.Y) > 0) blockedY = true;

            if (!moved && blockedX && blockedY) break;
        }

        return new(start, desired, current, current != desired, blockedX, blockedY);
    }

    public static bool CanOccupy(Vector2Data position, BoundsData worldBounds, CollisionShape? movementCollider, Func<Vector2Data, bool>? blockedAt = null)
    {
        if (!position.IsFinite || !worldBounds.IsValid) return false;
        var inside = movementCollider is null
            ? worldBounds.Contains(position)
            : CollisionGeometry.IsInside(movementCollider, position, worldBounds);
        return inside && (blockedAt is null || !blockedAt(position));
    }
}
