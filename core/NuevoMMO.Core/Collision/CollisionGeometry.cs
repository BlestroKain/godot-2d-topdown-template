namespace NuevoMMO.Core;

/// <summary>
/// Geometría determinista compartida por servidor, cliente y editor. No depende de Godot Physics.
/// El contacto tangencial no cuenta como penetración: Overlaps significa área/volumen solapado.
/// </summary>
public static class CollisionGeometry
{
    private const float Epsilon = 1e-5f;

    public static bool Overlaps(CollisionShape first, Vector2Data firstOrigin, CollisionShape second, Vector2Data secondOrigin)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        if (!firstOrigin.IsFinite || !secondOrigin.IsFinite) throw new ArgumentException("Origen de colisión no finito.");
        if (!first.BoundsAt(firstOrigin).Intersects(second.BoundsAt(secondOrigin))) return false;

        if (first is CompoundCollisionShape firstCompound)
            return firstCompound.Parts.Any(part => Overlaps(part, firstOrigin + firstCompound.Offset, second, secondOrigin));
        if (second is CompoundCollisionShape secondCompound)
            return secondCompound.Parts.Any(part => Overlaps(first, firstOrigin, part, secondOrigin + secondCompound.Offset));

        return (first, second) switch
        {
            (CircleCollisionShape a, CircleCollisionShape b) => CircleCircle(a, firstOrigin, b, secondOrigin),
            (BoxCollisionShape a, BoxCollisionShape b) => a.BoundsAt(firstOrigin).Intersects(b.BoundsAt(secondOrigin)),
            (CircleCollisionShape a, BoxCollisionShape b) => CircleBox(a, firstOrigin, b, secondOrigin),
            (BoxCollisionShape a, CircleCollisionShape b) => CircleBox(b, secondOrigin, a, firstOrigin),
            (CapsuleCollisionShape a, CircleCollisionShape b) => CapsuleCircle(a, firstOrigin, b, secondOrigin),
            (CircleCollisionShape a, CapsuleCollisionShape b) => CapsuleCircle(b, secondOrigin, a, firstOrigin),
            (CapsuleCollisionShape a, CapsuleCollisionShape b) => CapsuleCapsule(a, firstOrigin, b, secondOrigin),
            (CapsuleCollisionShape a, BoxCollisionShape b) => CapsulePolygon(a, firstOrigin, BoxPoints(b, secondOrigin)),
            (BoxCollisionShape a, CapsuleCollisionShape b) => CapsulePolygon(b, secondOrigin, BoxPoints(a, firstOrigin)),
            (ConvexPolygonCollisionShape a, ConvexPolygonCollisionShape b) => PolygonPolygon(a.WorldPoints(firstOrigin), b.WorldPoints(secondOrigin)),
            (ConvexPolygonCollisionShape a, BoxCollisionShape b) => PolygonPolygon(a.WorldPoints(firstOrigin), BoxPoints(b, secondOrigin)),
            (BoxCollisionShape a, ConvexPolygonCollisionShape b) => PolygonPolygon(BoxPoints(a, firstOrigin), b.WorldPoints(secondOrigin)),
            (CircleCollisionShape a, ConvexPolygonCollisionShape b) => CirclePolygon(a.CenterAt(firstOrigin), a.Radius, b.WorldPoints(secondOrigin)),
            (ConvexPolygonCollisionShape a, CircleCollisionShape b) => CirclePolygon(b.CenterAt(secondOrigin), b.Radius, a.WorldPoints(firstOrigin)),
            (CapsuleCollisionShape a, ConvexPolygonCollisionShape b) => CapsulePolygon(a, firstOrigin, b.WorldPoints(secondOrigin)),
            (ConvexPolygonCollisionShape a, CapsuleCollisionShape b) => CapsulePolygon(b, secondOrigin, a.WorldPoints(firstOrigin)),
            _ => throw new NotSupportedException($"Par de shapes no soportado: {first.GetType().Name}/{second.GetType().Name}")
        };
    }

    public static bool IsInside(CollisionShape shape, Vector2Data origin, BoundsData worldBounds)
    {
        ArgumentNullException.ThrowIfNull(shape);
        if (!origin.IsFinite) return false;
        return shape.BoundsAt(origin).IsInside(worldBounds);
    }

    public static bool OverlapsBounds(CollisionShape shape, Vector2Data origin, CollisionBounds bounds)
    {
        var box = new BoxCollisionShape(bounds.Width * .5f, bounds.Height * .5f, bounds.Center);
        return Overlaps(shape, origin, box, Vector2Data.Zero);
    }

    private static bool CircleCircle(CircleCollisionShape a, Vector2Data ao, CircleCollisionShape b, Vector2Data bo)
    {
        var radius = a.Radius + b.Radius;
        return a.CenterAt(ao).DistanceSquaredTo(b.CenterAt(bo)) < radius * radius;
    }

    private static bool CircleBox(CircleCollisionShape circle, Vector2Data circleOrigin, BoxCollisionShape box, Vector2Data boxOrigin)
    {
        var center = circle.CenterAt(circleOrigin);
        var bounds = box.BoundsAt(boxOrigin);
        var x = Math.Clamp(center.X, bounds.Left, bounds.Right);
        var y = Math.Clamp(center.Y, bounds.Top, bounds.Bottom);
        var dx = center.X - x;
        var dy = center.Y - y;
        return dx * dx + dy * dy < circle.Radius * circle.Radius;
    }

    private static bool CapsuleCircle(CapsuleCollisionShape capsule, Vector2Data capsuleOrigin, CircleCollisionShape circle, Vector2Data circleOrigin)
    {
        var radius = capsule.Radius + circle.Radius;
        return DistanceSquaredPointSegment(circle.CenterAt(circleOrigin), capsule.WorldA(capsuleOrigin), capsule.WorldB(capsuleOrigin)) < radius * radius;
    }

    private static bool CapsuleCapsule(CapsuleCollisionShape a, Vector2Data ao, CapsuleCollisionShape b, Vector2Data bo)
    {
        var radius = a.Radius + b.Radius;
        return DistanceSquaredSegmentSegment(a.WorldA(ao), a.WorldB(ao), b.WorldA(bo), b.WorldB(bo)) < radius * radius;
    }

    private static bool CirclePolygon(Vector2Data center, float radius, IReadOnlyList<Vector2Data> polygon)
    {
        if (PointInConvexPolygon(center, polygon)) return true;
        var radiusSquared = radius * radius;
        for (var index = 0; index < polygon.Count; index++)
            if (DistanceSquaredPointSegment(center, polygon[index], polygon[(index + 1) % polygon.Count]) < radiusSquared)
                return true;
        return false;
    }

    private static bool CapsulePolygon(CapsuleCollisionShape capsule, Vector2Data origin, IReadOnlyList<Vector2Data> polygon)
    {
        var a = capsule.WorldA(origin);
        var b = capsule.WorldB(origin);
        if (PointInConvexPolygon(a, polygon) || PointInConvexPolygon(b, polygon)) return true;
        var radiusSquared = capsule.Radius * capsule.Radius;
        for (var index = 0; index < polygon.Count; index++)
        {
            var c = polygon[index];
            var d = polygon[(index + 1) % polygon.Count];
            if (DistanceSquaredSegmentSegment(a, b, c, d) < radiusSquared) return true;
        }
        return polygon.Any(point => DistanceSquaredPointSegment(point, a, b) < radiusSquared);
    }

    private static bool PolygonPolygon(IReadOnlyList<Vector2Data> a, IReadOnlyList<Vector2Data> b)
        => !HasSeparatingAxis(a, b) && !HasSeparatingAxis(b, a);

    private static bool HasSeparatingAxis(IReadOnlyList<Vector2Data> source, IReadOnlyList<Vector2Data> other)
    {
        for (var index = 0; index < source.Count; index++)
        {
            var edge = source[(index + 1) % source.Count] - source[index];
            var axis = new Vector2Data(-edge.Y, edge.X);
            Project(source, axis, out var minA, out var maxA);
            Project(other, axis, out var minB, out var maxB);
            if (maxA <= minB + Epsilon || maxB <= minA + Epsilon) return true;
        }
        return false;
    }

    private static void Project(IReadOnlyList<Vector2Data> points, Vector2Data axis, out float minimum, out float maximum)
    {
        minimum = maximum = points[0].Dot(axis);
        for (var index = 1; index < points.Count; index++)
        {
            var value = points[index].Dot(axis);
            minimum = MathF.Min(minimum, value);
            maximum = MathF.Max(maximum, value);
        }
    }

    private static bool PointInConvexPolygon(Vector2Data point, IReadOnlyList<Vector2Data> polygon)
    {
        float sign = 0;
        for (var index = 0; index < polygon.Count; index++)
        {
            var a = polygon[index];
            var b = polygon[(index + 1) % polygon.Count];
            var cross = Cross(b - a, point - a);
            if (MathF.Abs(cross) <= Epsilon) continue;
            var current = MathF.Sign(cross);
            if (sign == 0) sign = current;
            else if (current != sign) return false;
        }
        return true;
    }

    private static Vector2Data[] BoxPoints(BoxCollisionShape box, Vector2Data origin)
    {
        var bounds = box.BoundsAt(origin);
        return
        [
            new(bounds.Left, bounds.Top), new(bounds.Right, bounds.Top),
            new(bounds.Right, bounds.Bottom), new(bounds.Left, bounds.Bottom)
        ];
    }

    private static float DistanceSquaredPointSegment(Vector2Data point, Vector2Data a, Vector2Data b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared;
        if (lengthSquared <= Epsilon) return point.DistanceSquaredTo(a);
        var t = Math.Clamp((point - a).Dot(ab) / lengthSquared, 0f, 1f);
        return point.DistanceSquaredTo(a + ab * t);
    }

    private static float DistanceSquaredSegmentSegment(Vector2Data a, Vector2Data b, Vector2Data c, Vector2Data d)
    {
        if (SegmentsIntersect(a, b, c, d)) return 0;
        return MathF.Min(
            MathF.Min(DistanceSquaredPointSegment(a, c, d), DistanceSquaredPointSegment(b, c, d)),
            MathF.Min(DistanceSquaredPointSegment(c, a, b), DistanceSquaredPointSegment(d, a, b)));
    }

    private static bool SegmentsIntersect(Vector2Data a, Vector2Data b, Vector2Data c, Vector2Data d)
    {
        var abC = Cross(b - a, c - a);
        var abD = Cross(b - a, d - a);
        var cdA = Cross(d - c, a - c);
        var cdB = Cross(d - c, b - c);
        return abC * abD < 0 && cdA * cdB < 0;
    }

    private static float Cross(Vector2Data a, Vector2Data b) => a.X * b.Y - a.Y * b.X;
}
