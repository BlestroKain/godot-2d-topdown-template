using NuevoMMO.Contracts;

namespace NuevoMMO.Application;

public sealed class SpatialIndex(float cellSize)
{
    private readonly Dictionary<(int X, int Y), HashSet<EntityId>> cells = [];
    private readonly Dictionary<EntityId, (int X, int Y)> locations = [];
    private (int X, int Y) Cell(WorldPosition position) => ((int)MathF.Floor(position.X / cellSize), (int)MathF.Floor(position.Y / cellSize));

    public void Update(EntityId id, WorldPosition position)
    {
        var cell = Cell(position);
        if (locations.TryGetValue(id, out var previous) && previous == cell) return;
        Remove(id);
        if (!cells.TryGetValue(cell, out var bucket)) cells[cell] = bucket = [];
        bucket.Add(id); locations[id] = cell;
    }

    public void Remove(EntityId id)
    {
        if (!locations.Remove(id, out var cell)) return;
        cells[cell].Remove(id);
        if (cells[cell].Count == 0) cells.Remove(cell);
    }

    public IEnumerable<EntityId> Query(WorldPosition center, float radius)
    {
        var min = Cell(new(center.X - radius, center.Y - radius));
        var max = Cell(new(center.X + radius, center.Y + radius));
        for (var x = min.X; x <= max.X; x++)
        for (var y = min.Y; y <= max.Y; y++)
            if (cells.TryGetValue((x, y), out var bucket))
                foreach (var id in bucket) yield return id;
    }
}
