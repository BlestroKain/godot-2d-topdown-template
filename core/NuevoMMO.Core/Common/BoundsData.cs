namespace NuevoMMO.Core;

public readonly record struct BoundsData(Vector2Data Minimum, Vector2Data Maximum)
{
    public bool IsValid => Minimum.IsFinite && Maximum.IsFinite && Minimum.X <= Maximum.X && Minimum.Y <= Maximum.Y;
    public float Width => Maximum.X - Minimum.X;
    public float Height => Maximum.Y - Minimum.Y;

    public Vector2Data Clamp(Vector2Data value) => new(
        Math.Clamp(value.X, Minimum.X, Maximum.X), Math.Clamp(value.Y, Minimum.Y, Maximum.Y));
}
