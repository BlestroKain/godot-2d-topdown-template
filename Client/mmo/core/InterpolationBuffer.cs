using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class InterpolationBuffer
{
    private readonly Interpolation interpolation = new();
    public void Add(double time, Vector2Data position) => interpolation.Add(time, position);
    public Vector2Data Sample(double time) => interpolation.Sample(time);
}
