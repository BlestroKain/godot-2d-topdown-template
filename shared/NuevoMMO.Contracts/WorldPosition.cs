namespace NuevoMMO.Contracts;

public readonly record struct WorldPosition(float X, float Y)
{
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y);
    public static WorldPosition Lerp(WorldPosition a, WorldPosition b, float weight)
        => new(a.X + (b.X - a.X) * weight, a.Y + (b.Y - a.Y) * weight);
}
