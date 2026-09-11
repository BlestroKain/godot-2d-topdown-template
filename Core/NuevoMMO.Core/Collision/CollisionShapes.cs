namespace NuevoMMO.Core;

/// <summary>Convención lógica del mundo. El movimiento sigue siendo continuo; esto no fuerza grid movement.</summary>
public static class WorldGrid
{
    public const int LogicalTileSize = 32;
}

public readonly record struct CollisionBounds(float Left, float Top, float Right, float Bottom)
{
    public CollisionBounds
    {
        if (!float.IsFinite(Left) || !float.IsFinite(Top) || !float.IsFinite(Right) || !float.IsFinite(Bottom) ||
            Left > Right || Top > Bottom)
            throw new ArgumentException("CollisionBounds inválido.");
    }

    public float Width => Right - Left;
    public float Height => Bottom - Top;
    public Vector2Data Center => new((Left + Right) * .5f, (Top + Bottom) * .5f);

    /// <summary>Contacto tangencial no cuenta como penetración.</summary>
    public bool Intersects(CollisionBounds other)
        => Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;

    public bool IsInside(BoundsData world)
        => world.IsValid && Left >= world.Minimum.X && Top >= world.Minimum.Y &&
           Right <= world.Maximum.X && Bottom <= world.Maximum.Y;

    public static CollisionBounds Union(CollisionBounds first, CollisionBounds second)
        => new(MathF.Min(first.Left, second.Left), MathF.Min(first.Top, second.Top),
            MathF.Max(first.Right, second.Right), MathF.Max(first.Bottom, second.Bottom));
}

/// <summary>
/// Geometría semántica independiente del sprite. Offset está expresado en coordenadas locales
/// respecto al origen lógico de la entidad.
/// </summary>
public abstract record CollisionShape
{
    protected CollisionShape(Vector2Data offset)
    {
        if (!offset.IsFinite) throw new ArgumentException("Offset de colisión no finito.", nameof(offset));
        Offset = offset;
    }

    public Vector2Data Offset { get; }
    public Vector2Data CenterAt(Vector2Data origin) => origin + Offset;
    public abstract CollisionBounds BoundsAt(Vector2Data origin);
}

public sealed record CircleCollisionShape : CollisionShape
{
    public CircleCollisionShape(float radius, Vector2Data offset = default) : base(offset)
    {
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Radius = radius;
    }

    public float Radius { get; }
    public override CollisionBounds BoundsAt(Vector2Data origin)
    {
        var center = CenterAt(origin);
        return new(center.X - Radius, center.Y - Radius, center.X + Radius, center.Y + Radius);
    }
}

public sealed record BoxCollisionShape : CollisionShape
{
    public BoxCollisionShape(float halfWidth, float halfHeight, Vector2Data offset = default) : base(offset)
    {
        if (!float.IsFinite(halfWidth) || halfWidth <= 0) throw new ArgumentOutOfRangeException(nameof(halfWidth));
        if (!float.IsFinite(halfHeight) || halfHeight <= 0) throw new ArgumentOutOfRangeException(nameof(halfHeight));
        HalfWidth = halfWidth;
        HalfHeight = halfHeight;
    }

    public float HalfWidth { get; }
    public float HalfHeight { get; }
    public override CollisionBounds BoundsAt(Vector2Data origin)
    {
        var center = CenterAt(origin);
        return new(center.X - HalfWidth, center.Y - HalfHeight, center.X + HalfWidth, center.Y + HalfHeight);
    }
}

/// <summary>Cápsula arbitrariamente orientada: segmento local A→B engrosado por Radius.</summary>
public sealed record CapsuleCollisionShape : CollisionShape
{
    public CapsuleCollisionShape(Vector2Data a, Vector2Data b, float radius, Vector2Data offset = default) : base(offset)
    {
        if (!a.IsFinite || !b.IsFinite || a == b) throw new ArgumentException("Segmento de cápsula inválido.");
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        A = a;
        B = b;
        Radius = radius;
    }

    public Vector2Data A { get; }
    public Vector2Data B { get; }
    public float Radius { get; }
    public Vector2Data WorldA(Vector2Data origin) => origin + Offset + A;
    public Vector2Data WorldB(Vector2Data origin) => origin + Offset + B;
    public override CollisionBounds BoundsAt(Vector2Data origin)
    {
        var a = WorldA(origin);
        var b = WorldB(origin);
        return new(MathF.Min(a.X, b.X) - Radius, MathF.Min(a.Y, b.Y) - Radius,
            MathF.Max(a.X, b.X) + Radius, MathF.Max(a.Y, b.Y) + Radius);
    }
}

/// <summary>Polígono convexo. Los puntos se expresan en coordenadas locales alrededor de Offset.</summary>
public sealed record ConvexPolygonCollisionShape : CollisionShape
{
    public ConvexPolygonCollisionShape(IEnumerable<Vector2Data> points, Vector2Data offset = default) : base(offset)
    {
        ArgumentNullException.ThrowIfNull(points);
        Points = points.ToArray();
        if (Points.Length < 3 || Points.Any(static point => !point.IsFinite))
            throw new ArgumentException("Un polígono de colisión necesita al menos tres puntos finitos.", nameof(points));
        if (!CollisionShapeValidation.IsStrictlyConvex(Points))
            throw new ArgumentException("El polígono de colisión debe ser convexo y no degenerado.", nameof(points));
    }

    public Vector2Data[] Points { get; }
    public Vector2Data[] WorldPoints(Vector2Data origin)
    {
        var translation = origin + Offset;
        return Points.Select(point => point + translation).ToArray();
    }

    public override CollisionBounds BoundsAt(Vector2Data origin)
    {
        var translation = origin + Offset;
        var first = Points[0] + translation;
        var left = first.X; var right = first.X; var top = first.Y; var bottom = first.Y;
        for (var index = 1; index < Points.Length; index++)
        {
            var point = Points[index] + translation;
            left = MathF.Min(left, point.X); right = MathF.Max(right, point.X);
            top = MathF.Min(top, point.Y); bottom = MathF.Max(bottom, point.Y);
        }
        return new(left, top, right, bottom);
    }
}

/// <summary>Una sola función semántica compuesta por varias primitivas.</summary>
public sealed record CompoundCollisionShape : CollisionShape
{
    public CompoundCollisionShape(IEnumerable<CollisionShape> parts, Vector2Data offset = default) : base(offset)
    {
        ArgumentNullException.ThrowIfNull(parts);
        Parts = parts.ToArray();
        if (Parts.Length == 0) throw new ArgumentException("Un collider compuesto necesita al menos una parte.", nameof(parts));
    }

    public CollisionShape[] Parts { get; }
    public override CollisionBounds BoundsAt(Vector2Data origin)
    {
        var compoundOrigin = origin + Offset;
        var bounds = Parts[0].BoundsAt(compoundOrigin);
        for (var index = 1; index < Parts.Length; index++)
            bounds = CollisionBounds.Union(bounds, Parts[index].BoundsAt(compoundOrigin));
        return bounds;
    }
}

/// <summary>
/// Roles físicos separados. El sprite no participa. Movimiento, daño, interacción y navegación
/// pueden usar geometrías diferentes y un mismo rol puede ser compuesto.
/// </summary>
public sealed record EntityCollisionProfile
{
    public EntityCollisionProfile(
        CollisionShape? movementCollider = null,
        IEnumerable<CollisionShape>? hurtboxes = null,
        CollisionShape? interactionShape = null,
        IEnumerable<CollisionShape>? hitboxes = null,
        float navigationRadius = 0,
        bool blocksMovement = false)
    {
        if (!float.IsFinite(navigationRadius) || navigationRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(navigationRadius));
        MovementCollider = movementCollider;
        Hurtboxes = hurtboxes?.ToArray() ?? [];
        InteractionShape = interactionShape;
        Hitboxes = hitboxes?.ToArray() ?? [];
        NavigationRadius = navigationRadius;
        BlocksMovement = blocksMovement;
    }

    public CollisionShape? MovementCollider { get; }
    public CollisionShape[] Hurtboxes { get; }
    public CollisionShape? InteractionShape { get; }
    public CollisionShape[] Hitboxes { get; }
    public float NavigationRadius { get; }
    public bool BlocksMovement { get; }
    public static EntityCollisionProfile Empty { get; } = new();
}

internal static class CollisionShapeValidation
{
    public static bool IsStrictlyConvex(IReadOnlyList<Vector2Data> points)
    {
        float sign = 0;
        for (var index = 0; index < points.Count; index++)
        {
            var a = points[index];
            var b = points[(index + 1) % points.Count];
            var c = points[(index + 2) % points.Count];
            var cross = Cross(b - a, c - b);
            if (MathF.Abs(cross) <= 1e-5f) return false;
            var current = MathF.Sign(cross);
            if (sign == 0) sign = current;
            else if (current != sign) return false;
        }
        return sign != 0;
    }

    private static float Cross(Vector2Data a, Vector2Data b) => a.X * b.Y - a.Y * b.X;
}
