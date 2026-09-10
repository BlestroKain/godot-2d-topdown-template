namespace NuevoMMO.Core;

/// <summary>
/// Vector bidimensional independiente del motor gráfico.
/// Utilizado para posiciones, direcciones, velocidades
/// y cálculos espaciales del dominio.
/// </summary>
public readonly record struct Vector2Data(float X, float Y)
{
    public static Vector2Data Zero => new(0f, 0f);
    public static Vector2Data One => new(1f, 1f);

    public static Vector2Data Up => new(0f, -1f);
    public static Vector2Data Down => new(0f, 1f);
    public static Vector2Data Left => new(-1f, 0f);
    public static Vector2Data Right => new(1f, 0f);

    public bool IsFinite =>
        float.IsFinite(X) &&
        float.IsFinite(Y);

    public bool IsZero =>
        X == 0f &&
        Y == 0f;

    public float LengthSquared =>
        X * X + Y * Y;

    public float Length =>
        MathF.Sqrt(LengthSquared);

    public Vector2Data Normalized()
    {
        var lengthSquared = LengthSquared;

        if (lengthSquared <= 0f || !float.IsFinite(lengthSquared))
            return Zero;

        var inverseLength = 1f / MathF.Sqrt(lengthSquared);

        return new Vector2Data(
            X * inverseLength,
            Y * inverseLength);
    }

    public Vector2Data ClampLength(float maxLength)
    {
        if (!float.IsFinite(maxLength) || maxLength < 0f)
            throw new ArgumentOutOfRangeException(
                nameof(maxLength),
                "La longitud máxima debe ser finita y no negativa.");

        var lengthSquared = LengthSquared;
        var maxLengthSquared = maxLength * maxLength;

        if (lengthSquared <= maxLengthSquared)
            return this;

        if (lengthSquared <= 0f || !float.IsFinite(lengthSquared))
            return Zero;

        var scale = maxLength / MathF.Sqrt(lengthSquared);

        return new Vector2Data(
            X * scale,
            Y * scale);
    }

    public float DistanceSquaredTo(Vector2Data other)
    {
        var deltaX = other.X - X;
        var deltaY = other.Y - Y;

        return deltaX * deltaX + deltaY * deltaY;
    }

    public float DistanceTo(Vector2Data other)
        => MathF.Sqrt(DistanceSquaredTo(other));

    public float Dot(Vector2Data other)
        => X * other.X + Y * other.Y;

    public static Vector2Data Lerp(
        Vector2Data from,
        Vector2Data to,
        float weight)
    {
        return new Vector2Data(
            from.X + (to.X - from.X) * weight,
            from.Y + (to.Y - from.Y) * weight);
    }

    public static Vector2Data operator +(
        Vector2Data left,
        Vector2Data right)
        => new(
            left.X + right.X,
            left.Y + right.Y);

    public static Vector2Data operator -(
        Vector2Data left,
        Vector2Data right)
        => new(
            left.X - right.X,
            left.Y - right.Y);

    public static Vector2Data operator -(
        Vector2Data value)
        => new(
            -value.X,
            -value.Y);

    public static Vector2Data operator *(
        Vector2Data value,
        float scalar)
        => new(
            value.X * scalar,
            value.Y * scalar);

    public static Vector2Data operator *(
        float scalar,
        Vector2Data value)
        => value * scalar;

    public static Vector2Data operator /(
        Vector2Data value,
        float scalar)
    {
        if (scalar == 0f)
            throw new DivideByZeroException(
                "No se puede dividir Vector2Data entre cero.");

        return new Vector2Data(
            value.X / scalar,
            value.Y / scalar);
    }
}