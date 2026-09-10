using NuevoMMO.Contracts;

namespace NuevoMMO.Domain;

public sealed class MapDefinition
{
    public MapId Id { get; }
    public float Width { get; }
    public float Height { get; }

    public MapDefinition(MapId id, float width, float height)
    {
        if (id.Value == Guid.Empty || !float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0)
            throw new ArgumentException("Mapa inválido.");
        Id = id; Width = width; Height = height;
    }

    public WorldPosition Clamp(WorldPosition position)
        => new(Math.Clamp(position.X, 0, Width), Math.Clamp(position.Y, 0, Height));
}
