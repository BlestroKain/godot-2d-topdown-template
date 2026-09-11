using System.Runtime.CompilerServices;
using NuevoMMO.Core;

namespace NuevoMMO.Server.Systems;

public sealed record CompiledMapCollision(
    Guid Id,
    CollisionShape Shape,
    bool BlocksMovement,
    bool BlocksProjectiles,
    bool BlocksVision,
    bool NavigationObstacle);

/// <summary>
/// Índice runtime derivado exclusivamente de MapDefinition.Content.Collisions. No persiste un segundo
/// formato de colisión: compila una vez la geometría de autoría y la consulta por AABB + geometría exacta.
/// </summary>
public sealed class MapCollisionRuntime
{
    private const float CellSize = WorldGrid.LogicalTileSize;
    private static readonly CircleCollisionShape PointProbe = new(.001f);
    private static readonly ConditionalWeakTable<MapDefinition, MapCollisionRuntime> Cache = new();

    private readonly CompiledMapCollision[] collisions;
    private readonly Dictionary<(int X, int Y), int[]> cells;

    private MapCollisionRuntime(MapDefinition map)
    {
        ArgumentNullException.ThrowIfNull(map);
        collisions = map.Content.Collisions.Select(static definition => new CompiledMapCollision(
            definition.Id,
            MapCollisionShapeCompiler.Compile(definition.Shape),
            definition.BlocksMovement,
            definition.BlocksProjectiles,
            definition.BlocksVision,
            definition.NavigationObstacle)).ToArray();
        cells = BuildIndex(collisions);
    }

    public IReadOnlyList<CompiledMapCollision> Collisions => collisions;

    public static MapCollisionRuntime For(MapDefinition map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return Cache.GetValue(map, static definition => new MapCollisionRuntime(definition));
    }

    public bool BlocksMovement(CollisionShape? mover, Vector2Data origin)
        => OverlapsFlag(mover, origin, static collision => collision.BlocksMovement);

    public bool BlocksProjectile(CollisionShape? projectile, Vector2Data origin)
        => OverlapsFlag(projectile, origin, static collision => collision.BlocksProjectiles);

    public bool BlocksVision(CollisionShape? probe, Vector2Data origin)
        => OverlapsFlag(probe, origin, static collision => collision.BlocksVision);

    /// <summary>
    /// Consulta LoS contra la geometría exacta marcada BlocksVision. Usa una cápsula muy fina en vez de
    /// muestrear celdas, por lo que paredes delgadas y polígonos conservan la misma semántica que el resto.
    /// </summary>
    public bool BlocksVisionSegment(Vector2Data from, Vector2Data to, float radius = .001f)
    {
        if (!from.IsFinite || !to.IsFinite) return true;
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (from == to) return BlocksVision(PointProbe, from);
        var segment = new CapsuleCollisionShape(Vector2Data.Zero, to - from, radius);
        return OverlapsFlag(segment, from, static collision => collision.BlocksVision);
    }

    public bool HasLineOfSight(Vector2Data from, Vector2Data to, float radius = .001f)
        => !BlocksVisionSegment(from, to, radius);

    public bool IsNavigationObstacle(CollisionShape? probe, Vector2Data origin)
        => OverlapsFlag(probe, origin, static collision => collision.NavigationObstacle);

    private bool OverlapsFlag(
        CollisionShape? probe,
        Vector2Data origin,
        Func<CompiledMapCollision, bool> enabled)
    {
        if (!origin.IsFinite) return true;
        var shape = probe ?? PointProbe;
        var bounds = shape.BoundsAt(origin);
        foreach (var index in Query(bounds))
        {
            var collision = collisions[index];
            if (!enabled(collision)) continue;
            if (CollisionGeometry.Overlaps(shape, origin, collision.Shape, Vector2Data.Zero)) return true;
        }
        return false;
    }

    private IEnumerable<int> Query(CollisionBounds bounds)
    {
        if (collisions.Length == 0) yield break;
        var minX = Cell(bounds.Left);
        var maxX = Cell(bounds.Right);
        var minY = Cell(bounds.Top);
        var maxY = Cell(bounds.Bottom);
        var seen = new HashSet<int>();
        for (var x = minX; x <= maxX; x++)
        for (var y = minY; y <= maxY; y++)
        {
            if (!cells.TryGetValue((x, y), out var bucket)) continue;
            foreach (var index in bucket)
                if (seen.Add(index)) yield return index;
        }
    }

    private static Dictionary<(int X, int Y), int[]> BuildIndex(IReadOnlyList<CompiledMapCollision> source)
    {
        var mutable = new Dictionary<(int X, int Y), List<int>>();
        for (var index = 0; index < source.Count; index++)
        {
            var bounds = source[index].Shape.BoundsAt(Vector2Data.Zero);
            var minX = Cell(bounds.Left);
            var maxX = Cell(bounds.Right);
            var minY = Cell(bounds.Top);
            var maxY = Cell(bounds.Bottom);
            for (var x = minX; x <= maxX; x++)
            for (var y = minY; y <= maxY; y++)
            {
                if (!mutable.TryGetValue((x, y), out var bucket)) mutable[(x, y)] = bucket = [];
                bucket.Add(index);
            }
        }
        return mutable.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray());
    }

    private static int Cell(float coordinate) => (int)MathF.Floor(coordinate / CellSize);
}
