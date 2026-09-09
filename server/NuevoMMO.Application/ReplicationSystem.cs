using NuevoMMO.Contracts;
using NuevoMMO.Domain;

namespace NuevoMMO.Application;

public sealed class ReplicationSystem
{
    public WorldSnapshot Project(PlayerSession viewer, long tick, IEnumerable<PlayerEntity> candidates, float radius)
    {
        var current = new Dictionary<EntityId, EntityProjection>();
        foreach (var entity in candidates)
        {
            var dx = entity.Position.X - viewer.Player.Position.X;
            var dy = entity.Position.Y - viewer.Player.Position.Y;
            if (entity.Instance != viewer.Player.Instance || dx * dx + dy * dy > radius * radius) continue;
            current.Add(entity.Id, new(entity.Id, entity.Name, entity.Position, entity.Velocity, "template.player"));
        }
        var changes = current.Where(pair => !viewer.Baseline.TryGetValue(pair.Key, out var old) || old != pair.Value)
            .Select(pair => pair.Value).ToArray();
        var leaves = viewer.Baseline.Keys.Where(id => !current.ContainsKey(id)).ToArray();
        var full = !viewer.HasSnapshot;
        viewer.Baseline.Clear();
        foreach (var pair in current) viewer.Baseline.Add(pair.Key, pair.Value);
        viewer.HasSnapshot = true;
        return new(tick, full, new(viewer.Player.Id, viewer.Player.Position, viewer.Player.Inputs.LastProcessed), changes, leaves);
    }
}
