namespace NuevoMMO.Core;

public readonly record struct Vector2Data(float X, float Y)
{
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y);
    public float LengthSquared => X * X + Y * Y;
    public static Vector2Data Lerp(Vector2Data from, Vector2Data to, float weight)
        => new(from.X + (to.X - from.X) * weight, from.Y + (to.Y - from.Y) * weight);
}

public readonly record struct Vector2IntData(int X, int Y);
public readonly record struct BoundsData(Vector2Data Minimum, Vector2Data Maximum)
{
    public bool IsValid => Minimum.IsFinite && Maximum.IsFinite && Minimum.X <= Maximum.X && Minimum.Y <= Maximum.Y;
    public Vector2Data Clamp(Vector2Data value) => new(
        Math.Clamp(value.X, Minimum.X, Maximum.X), Math.Clamp(value.Y, Minimum.Y, Maximum.Y));
}

public enum Direction : byte { None, Up, Down, Left, Right }
public enum PrimaryAttributeId : byte { Strength, Intelligence, Agility, Spirit, Vitality }
public enum SecondaryAttributeId : byte { Luck }
