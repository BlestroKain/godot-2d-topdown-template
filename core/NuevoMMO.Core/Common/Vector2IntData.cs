namespace NuevoMMO.Core;

/// <summary>
/// Vector bidimensional entero para coordenadas discretas,
/// tamaños de tile, chunks e índices espaciales.
/// </summary>
public readonly record struct Vector2IntData(int X, int Y)
{
    public static Vector2IntData Zero => new(0, 0);
    public static Vector2IntData One => new(1, 1);

    public static Vector2IntData Up => new(0, -1);
    public static Vector2IntData Down => new(0, 1);
    public static Vector2IntData Left => new(-1, 0);
    public static Vector2IntData Right => new(1, 0);

    public bool IsZero => X == 0 && Y == 0;

    public long LengthSquared =>
        (long)X * X + (long)Y * Y;

    public static Vector2IntData operator +(
        Vector2IntData left,
        Vector2IntData right)
        => new(
            left.X + right.X,
            left.Y + right.Y);

    public static Vector2IntData operator -(
        Vector2IntData left,
        Vector2IntData right)
        => new(
            left.X - right.X,
            left.Y - right.Y);

    public static Vector2IntData operator -(
        Vector2IntData value)
        => new(
            -value.X,
            -value.Y);

    public static Vector2IntData operator *(
        Vector2IntData value,
        int scalar)
        => new(
            value.X * scalar,
            value.Y * scalar);
}