using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class SnapshotBuffer
{
    private readonly List<(long Tick, EntityState State)> samples = [];
    public void Add(long tick, EntityState state)
    {
        if (samples.Count > 0 && tick <= samples[^1].Tick) return;
        samples.Add((tick, state));
        if (samples.Count > 32) samples.RemoveAt(0);
    }
}
