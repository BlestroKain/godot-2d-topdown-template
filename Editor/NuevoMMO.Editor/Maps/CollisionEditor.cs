using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Edita geometría continua de colisión. No existe autoridad de blocked-cells.
/// </summary>
public sealed class CollisionEditor
{
    private MapDocument? document;
    public IReadOnlyList<MapCollisionDefinition> Shapes => RequireDocument().Collisions;

    public void Bind(MapDocument map) => document = map ?? throw new ArgumentNullException(nameof(map));

    public MapCollisionDefinition AddRectangle(Vector2Data center, Vector2Data size)
        => Add(new MapShapeDefinition(MapShapeKind.Rectangle, center, size));

    public MapCollisionDefinition AddCircle(Vector2Data center, float radius)
        => Add(new MapShapeDefinition(MapShapeKind.Circle, center, radius: radius));

    public MapCollisionDefinition AddPolygon(Vector2Data center, params Vector2Data[] points)
        => Add(new MapShapeDefinition(MapShapeKind.Polygon, center, points: points));

    public bool Remove(Guid id) => RequireDocument().Collisions.RemoveAll(collision => collision.Id == id) > 0;

    private MapCollisionDefinition Add(MapShapeDefinition shape)
    {
        var collision = new MapCollisionDefinition(Guid.NewGuid(), shape);
        RequireDocument().Collisions.Add(collision);
        return collision;
    }

    private MapDocument RequireDocument() => document ?? throw new InvalidOperationException("No hay mapa abierto.");
}
