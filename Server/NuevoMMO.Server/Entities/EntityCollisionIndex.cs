using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

/// <summary>
/// Broadphase autoritativo de cuerpos dinámicos. Usa celdas lógicas de 32 unidades únicamente como
/// índice espacial; la decisión final siempre se toma con la geometría continua exacta.
/// Las referencias son débiles para no convertir el índice en dueño del ciclo de vida de una entidad.
/// </summary>
public static class EntityCollisionIndex
{
    private const float CellSize = WorldGrid.LogicalTileSize;
    private static readonly object Gate = new();
    private static readonly Dictionary<(long Map, int X, int Y), Dictionary<long, WeakReference<Entity>>> Cells = [];
    private static readonly Dictionary<long, IndexedEntity> Indexed = [];

    public static void Update(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        lock (Gate)
        {
            RemoveCore(entity.Id.Value);
            var collider = entity.CollisionProfile.MovementCollider;
            if (collider is null) return;

            var occupied = CellsFor(collider.BoundsAt(entity.Position)).ToArray();
            var weak = new WeakReference<Entity>(entity);
            foreach (var cell in occupied)
            {
                var key = (entity.MapInstanceId.Value, cell.X, cell.Y);
                if (!Cells.TryGetValue(key, out var bucket)) Cells[key] = bucket = [];
                bucket[entity.Id.Value] = weak;
            }
            Indexed[entity.Id.Value] = new(entity.MapInstanceId.Value, occupied, weak);
        }
    }

    public static void Remove(EntityId id)
    {
        lock (Gate) RemoveCore(id.Value);
    }

    public static bool BlocksMovement(Entity mover, Vector2Data prospectivePosition)
    {
        ArgumentNullException.ThrowIfNull(mover);
        if (!prospectivePosition.IsFinite) return true;
        var moverCollider = mover.CollisionProfile.MovementCollider;
        if (moverCollider is null) return false;

        lock (Gate)
        {
            var candidates = new Dictionary<long, WeakReference<Entity>>();
            foreach (var cell in CellsFor(moverCollider.BoundsAt(prospectivePosition)))
            {
                var key = (mover.MapInstanceId.Value, cell.X, cell.Y);
                if (!Cells.TryGetValue(key, out var bucket)) continue;
                foreach (var pair in bucket)
                    if (pair.Key != mover.Id.Value) candidates[pair.Key] = pair.Value;
            }

            foreach (var pair in candidates)
            {
                if (!pair.Value.TryGetTarget(out var obstacle))
                {
                    RemoveCore(pair.Key);
                    continue;
                }
                if (obstacle.MapInstanceId != mover.MapInstanceId || !BlocksMovementNow(obstacle)) continue;
                var obstacleCollider = obstacle.CollisionProfile.MovementCollider;
                if (obstacleCollider is null) continue;
                if (CollisionGeometry.Overlaps(
                    moverCollider, prospectivePosition,
                    obstacleCollider, obstacle.Position))
                    return true;
            }
        }
        return false;
    }

    private static bool BlocksMovementNow(Entity entity)
        => entity switch
        {
            ResourceEntity resource => resource.BlocksMovement && resource.CollisionProfile.BlocksMovement,
            _ => entity.CollisionProfile.BlocksMovement
        };

    private static IEnumerable<(int X, int Y)> CellsFor(CollisionBounds bounds)
    {
        var minX = Cell(bounds.Left);
        var maxX = Cell(bounds.Right);
        var minY = Cell(bounds.Top);
        var maxY = Cell(bounds.Bottom);
        for (var x = minX; x <= maxX; x++)
        for (var y = minY; y <= maxY; y++)
            yield return (x, y);
    }

    private static int Cell(float value) => (int)MathF.Floor(value / CellSize);

    private static void RemoveCore(long id)
    {
        if (!Indexed.Remove(id, out var entry)) return;
        foreach (var cell in entry.Cells)
        {
            var key = (entry.Map, cell.X, cell.Y);
            if (!Cells.TryGetValue(key, out var bucket)) continue;
            bucket.Remove(id);
            if (bucket.Count == 0) Cells.Remove(key);
        }
    }

    private sealed record IndexedEntity(
        long Map,
        (int X, int Y)[] Cells,
        WeakReference<Entity> Reference);
}
