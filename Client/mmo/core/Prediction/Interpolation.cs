using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class Interpolation
{
    private readonly List<(double Time, Vector2Data Position)> samples = [];
    public void Add(double time, Vector2Data position)
    {
        if (samples.Count > 0 && time <= samples[^1].Time) return;
        samples.Add((time, position));
        if (samples.Count > 32) samples.RemoveAt(0);
    }

    public Vector2Data Sample(double time)
    {
        if (samples.Count == 0) return default;
        if (time <= samples[0].Time) return samples[0].Position;
        for (var index = 1; index < samples.Count; index++)
        {
            var after = samples[index];
            var before = samples[index - 1];
            if (time <= after.Time) return Vector2Data.Lerp(before.Position, after.Position,
                (float)((time - before.Time) / (after.Time - before.Time)));
        }
        return samples[^1].Position;
    }
}
