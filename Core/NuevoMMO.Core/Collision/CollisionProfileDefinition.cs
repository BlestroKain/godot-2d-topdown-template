namespace NuevoMMO.Core;

public enum CollisionShapeDefinitionKind : byte
{
    Circle,
    Box,
    Capsule,
    ConvexPolygon,
    Compound
}

/// <summary>
/// DTO serializable de una forma semántica. Mantiene la Definition independiente de Godot y evita
/// serializar la jerarquía abstracta CollisionShape directamente.
/// </summary>
public sealed record CollisionShapeDefinition
{
    public CollisionShapeDefinition(
        CollisionShapeDefinitionKind kind,
        Vector2Data offset = default,
        float radius = 0,
        float halfWidth = 0,
        float halfHeight = 0,
        Vector2Data a = default,
        Vector2Data b = default,
        Vector2Data[]? points = null,
        CollisionShapeDefinition[]? parts = null)
    {
        if (!offset.IsFinite || !float.IsFinite(radius) || !float.IsFinite(halfWidth) ||
            !float.IsFinite(halfHeight) || !a.IsFinite || !b.IsFinite)
            throw new ArgumentException("Geometría de colisión no finita.");

        var normalizedPoints = points?.ToArray() ?? [];
        var normalizedParts = parts?.ToArray() ?? [];
        if (normalizedPoints.Any(static point => !point.IsFinite))
            throw new ArgumentException("Points contiene valores no finitos.", nameof(points));
        if (normalizedParts.Any(static part => part is null))
            throw new ArgumentException("Parts contiene valores nulos.", nameof(parts));

        switch (kind)
        {
            case CollisionShapeDefinitionKind.Circle when radius <= 0:
                throw new ArgumentOutOfRangeException(nameof(radius));
            case CollisionShapeDefinitionKind.Box when halfWidth <= 0 || halfHeight <= 0:
                throw new ArgumentException("Box requiere semiejes positivos.");
            case CollisionShapeDefinitionKind.Capsule when radius <= 0 || a == b:
                throw new ArgumentException("Capsule requiere segmento y radio válidos.");
            case CollisionShapeDefinitionKind.ConvexPolygon when normalizedPoints.Length < 3:
                throw new ArgumentException("ConvexPolygon requiere al menos tres puntos.", nameof(points));
            case CollisionShapeDefinitionKind.Compound when normalizedParts.Length == 0:
                throw new ArgumentException("Compound requiere al menos una parte.", nameof(parts));
        }

        Kind = kind;
        Offset = offset;
        Radius = radius;
        HalfWidth = halfWidth;
        HalfHeight = halfHeight;
        A = a;
        B = b;
        Points = normalizedPoints;
        Parts = normalizedParts;
    }

    public CollisionShapeDefinitionKind Kind { get; }
    public Vector2Data Offset { get; }
    public float Radius { get; }
    public float HalfWidth { get; }
    public float HalfHeight { get; }
    public Vector2Data A { get; }
    public Vector2Data B { get; }
    public Vector2Data[] Points { get; }
    public CollisionShapeDefinition[] Parts { get; }

    public CollisionShape ToRuntime() => Kind switch
    {
        CollisionShapeDefinitionKind.Circle => new CircleCollisionShape(Radius, Offset),
        CollisionShapeDefinitionKind.Box => new BoxCollisionShape(HalfWidth, HalfHeight, Offset),
        CollisionShapeDefinitionKind.Capsule => new CapsuleCollisionShape(A, B, Radius, Offset),
        CollisionShapeDefinitionKind.ConvexPolygon => new ConvexPolygonCollisionShape(Points, Offset),
        CollisionShapeDefinitionKind.Compound => new CompoundCollisionShape(Parts.Select(static part => part.ToRuntime()), Offset),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Tipo de collider desconocido.")
    };

    public static CollisionShapeDefinition Box(float halfWidth, float halfHeight, Vector2Data offset = default)
        => new(CollisionShapeDefinitionKind.Box, offset, halfWidth: halfWidth, halfHeight: halfHeight);

    public static CollisionShapeDefinition Circle(float radius, Vector2Data offset = default)
        => new(CollisionShapeDefinitionKind.Circle, offset, radius: radius);
}

/// <summary>Definition editable de los cinco roles físicos de una entidad.</summary>
public sealed record EntityCollisionProfileDefinition
{
    public EntityCollisionProfileDefinition(
        CollisionShapeDefinition? movementCollider = null,
        CollisionShapeDefinition[]? hurtboxes = null,
        CollisionShapeDefinition? interactionShape = null,
        CollisionShapeDefinition[]? hitboxes = null,
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

    public CollisionShapeDefinition? MovementCollider { get; }
    public CollisionShapeDefinition[] Hurtboxes { get; }
    public CollisionShapeDefinition? InteractionShape { get; }
    public CollisionShapeDefinition[] Hitboxes { get; }
    public float NavigationRadius { get; }
    public bool BlocksMovement { get; }

    public EntityCollisionProfile ToRuntime() => new(
        MovementCollider?.ToRuntime(),
        Hurtboxes.Select(static shape => shape.ToRuntime()),
        InteractionShape?.ToRuntime(),
        Hitboxes.Select(static shape => shape.ToRuntime()),
        NavigationRadius,
        BlocksMovement);
}

/// <summary>Perfiles base publicados por el motor. No dependen del frame visual.</summary>
public static class CanonicalCollisionProfiles
{
    /// <summary>
    /// Pie físico del personaje 48×64. El origen lógico coincide con el ancla del sprite y el cuerpo
    /// físico ocupa solo los pies; no se usa el rectángulo visual completo.
    /// </summary>
    public static EntityCollisionProfileDefinition Player { get; } = new(
        movementCollider: CollisionShapeDefinition.Box(7, 4, new Vector2Data(0, 18)),
        navigationRadius: 10,
        blocksMovement: false);
}
