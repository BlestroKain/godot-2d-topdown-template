namespace NuevoMMO.Core;

/// <summary>
/// Adapta la geometría canónica de MapDefinition al kernel semántico de colisión compartido.
/// MapShapeDefinition permanece como formato de autoría/persistencia; CollisionShape es la forma
/// compilada que consumen servidor, cliente y herramientas.
/// </summary>
public static class MapCollisionShapeCompiler
{
    private const float Epsilon = 1e-5f;

    public static CollisionShape Compile(MapShapeDefinition shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        return shape.Kind switch
        {
            MapShapeKind.Rectangle => new BoxCollisionShape(shape.Size.X * .5f, shape.Size.Y * .5f, shape.Center),
            MapShapeKind.Circle => new CircleCollisionShape(shape.Radius, shape.Center),
            MapShapeKind.Polygon => CompilePolygon(shape.Points),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape.Kind, "Tipo de geometría de mapa desconocido.")
        };
    }

    /// <summary>
    /// Los puntos de MapShapeDefinition.Polygon son coordenadas de mundo, tal como los consumen
    /// regiones/portales existentes. Un polígono cóncavo se descompone determinísticamente en
    /// triángulos convexos para no crear un segundo algoritmo de colisión.
    /// </summary>
    private static CollisionShape CompilePolygon(IReadOnlyList<Vector2Data> authoredPoints)
    {
        if (authoredPoints.Count < 3) throw new InvalidDataException("El polígono necesita al menos tres puntos.");
        var points = authoredPoints.ToList();
        if (points.Count > 3 && points[0] == points[^1]) points.RemoveAt(points.Count - 1);
        if (points.Count < 3 || points.Any(static point => !point.IsFinite))
            throw new InvalidDataException("El polígono contiene puntos inválidos.");

        var area = SignedArea(points);
        if (MathF.Abs(area) <= Epsilon) throw new InvalidDataException("El polígono de colisión es degenerado.");

        try
        {
            return new ConvexPolygonCollisionShape(points);
        }
        catch (ArgumentException)
        {
            // El formato de mapa admite polígonos cóncavos. Los convertimos a un collider compuesto.
        }

        var clockwise = area < 0;
        var remaining = Enumerable.Range(0, points.Count).ToList();
        var triangles = new List<CollisionShape>(points.Count - 2);
        var safety = points.Count * points.Count;

        while (remaining.Count > 3 && safety-- > 0)
        {
            var clipped = false;
            for (var cursor = 0; cursor < remaining.Count; cursor++)
            {
                var previous = remaining[(cursor - 1 + remaining.Count) % remaining.Count];
                var current = remaining[cursor];
                var next = remaining[(cursor + 1) % remaining.Count];
                var a = points[previous];
                var b = points[current];
                var c = points[next];
                var cross = Cross(b - a, c - b);
                if (clockwise ? cross >= -Epsilon : cross <= Epsilon) continue;

                var containsOther = false;
                foreach (var candidate in remaining)
                {
                    if (candidate == previous || candidate == current || candidate == next) continue;
                    if (!PointInTriangle(points[candidate], a, b, c)) continue;
                    containsOther = true;
                    break;
                }
                if (containsOther) continue;

                triangles.Add(new ConvexPolygonCollisionShape([a, b, c]));
                remaining.RemoveAt(cursor);
                clipped = true;
                break;
            }

            if (!clipped)
                throw new InvalidDataException("El polígono de colisión no es simple o no puede triangularse.");
        }

        if (remaining.Count != 3)
            throw new InvalidDataException("No se pudo completar la triangulación del polígono de colisión.");
        triangles.Add(new ConvexPolygonCollisionShape(
            [points[remaining[0]], points[remaining[1]], points[remaining[2]]]));
        return triangles.Count == 1 ? triangles[0] : new CompoundCollisionShape(triangles);
    }

    private static float SignedArea(IReadOnlyList<Vector2Data> points)
    {
        var sum = 0f;
        for (var index = 0; index < points.Count; index++)
        {
            var a = points[index];
            var b = points[(index + 1) % points.Count];
            sum += a.X * b.Y - b.X * a.Y;
        }
        return sum * .5f;
    }

    private static bool PointInTriangle(Vector2Data point, Vector2Data a, Vector2Data b, Vector2Data c)
    {
        var ab = Cross(b - a, point - a);
        var bc = Cross(c - b, point - b);
        var ca = Cross(a - c, point - c);
        var hasNegative = ab < -Epsilon || bc < -Epsilon || ca < -Epsilon;
        var hasPositive = ab > Epsilon || bc > Epsilon || ca > Epsilon;
        return !(hasNegative && hasPositive);
    }

    private static float Cross(Vector2Data a, Vector2Data b) => a.X * b.Y - a.Y * b.X;
}
