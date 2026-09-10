namespace NuevoMMO.Core;

public readonly record struct Vector2Data(float X, float Y)
{
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y);
    public float LengthSquared => X * X + Y * Y;

    public static Vector2Data Lerp(Vector2Data from, Vector2Data to, float weight)
        => new(from.X + (to.X - from.X) * weight, from.Y + (to.Y - from.Y) * weight);
}
