using NuevoMMO.Core;

namespace NuevoMMO.Client;

/// <summary>
/// Resolución de objetivo de combate en el cliente. No aplica daño: solo elige un Mob visible.
/// </summary>
public static class CombatTargeting
{
    public const float DefaultRange = 256f;

    public static MobState? NearestMob(
        IEnumerable<EntityState> entities,
        Vector2Data origin,
        Func<MobState, bool>? predicate = null,
        float maxRange = DefaultRange)
    {
        ArgumentNullException.ThrowIfNull(entities);
        if (!origin.IsFinite) throw new ArgumentException("Origen no finito.", nameof(origin));
        if (!float.IsFinite(maxRange) || maxRange < 0) throw new ArgumentOutOfRangeException(nameof(maxRange));

        var maxRangeSquared = maxRange * maxRange;
        MobState? best = null;
        var bestDistance = maxRangeSquared;
        foreach (var entity in entities)
        {
            if (entity is not MobState mob) continue;
            if (predicate is not null && !predicate(mob)) continue;
            var dx = mob.Position.X - origin.X;
            var dy = mob.Position.Y - origin.Y;
            var distance = dx * dx + dy * dy;
            if (distance > bestDistance) continue;
            best = mob;
            bestDistance = distance;
        }

        return best;
    }

    public static MobState? CycleMob(
        IEnumerable<EntityState> entities,
        Vector2Data origin,
        EntityId? current,
        float maxRange = DefaultRange)
    {
        ArgumentNullException.ThrowIfNull(entities);
        if (!origin.IsFinite) throw new ArgumentException("Origen no finito.", nameof(origin));
        if (!float.IsFinite(maxRange) || maxRange < 0) throw new ArgumentOutOfRangeException(nameof(maxRange));

        var maxRangeSquared = maxRange * maxRange;
        var ordered = new List<MobState>();
        foreach (var entity in entities)
        {
            if (entity is not MobState mob) continue;
            var dx = mob.Position.X - origin.X;
            var dy = mob.Position.Y - origin.Y;
            if (dx * dx + dy * dy > maxRangeSquared) continue;
            ordered.Add(mob);
        }

        if (ordered.Count == 0) return null;
        ordered.Sort(static (left, right) => left.Id.Value.CompareTo(right.Id.Value));
        if (current is not { } currentId)
            return ordered[0];
        var index = ordered.FindIndex(mob => mob.Id == currentId);
        return ordered[(index + 1) % ordered.Count];
    }
}
