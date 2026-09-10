using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public abstract class Entity
{
    protected Entity(EntityId id, MapInstanceId mapInstance, Vector2Data position, ContentKey visualKey, string displayName)
    {
        if (id.Value <= 0) throw new ArgumentException("EntityId inválido.", nameof(id));
        if (mapInstance.Value <= 0) throw new ArgumentException("MapInstanceId inválido.", nameof(mapInstance));
        if (!position.IsFinite) throw new ArgumentException("Posición inválida.", nameof(position));
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Nombre vacío.", nameof(displayName));
        Id = id;
        MapInstanceId = mapInstance;
        Position = position;
        VisualKey = visualKey;
        DisplayName = displayName.Trim();
    }

    public EntityId Id { get; }
    public MapInstanceId MapInstanceId { get; private set; }
    public Vector2Data Position { get; private set; }
    public Vector2Data Velocity { get; private set; }
    public Direction Direction { get; protected set; }
    public ContentKey VisualKey { get; private set; }
    public string DisplayName { get; }

    public void MoveTo(Vector2Data position, Vector2Data velocity, Direction? facing = null)
    {
        if (!position.IsFinite || !velocity.IsFinite) throw new ArgumentException("Movimiento inválido.");
        Position = position;
        Velocity = velocity;
        if (facing is { } value && value != Direction.None) Direction = value;
        else if (MathF.Abs(velocity.X) + MathF.Abs(velocity.Y) > .001f)
            Direction = MathF.Abs(velocity.X) >= MathF.Abs(velocity.Y)
                ? (velocity.X < 0 ? Direction.Left : Direction.Right)
                : (velocity.Y < 0 ? Direction.Up : Direction.Down);
    }

    public void SetMapInstance(MapInstanceId mapInstance)
    {
        if (mapInstance.Value <= 0) throw new ArgumentException("MapInstanceId inválido.", nameof(mapInstance));
        MapInstanceId = mapInstance;
    }

    protected void SetVisualKey(ContentKey visualKey)
    {
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));
        VisualKey = visualKey;
    }

    public abstract EntityState ToState();
}
