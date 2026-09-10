namespace NuevoMMO.Core;

/// <summary>
/// Representa un área rectangular continua en coordenadas de mundo.
/// </summary>
public readonly record struct BoundsData(
    Vector2Data Minimum,
    Vector2Data Maximum)
{
    /// <summary>
    /// Indica si los límites contienen valores finitos
    /// y forman un área con dimensiones positivas.
    /// </summary>
    public bool IsValid =>
        Minimum.IsFinite &&
        Maximum.IsFinite &&
        Minimum.X < Maximum.X &&
        Minimum.Y < Maximum.Y;

    public float Width => Maximum.X - Minimum.X;

    public float Height => Maximum.Y - Minimum.Y;

    public Vector2Data Size => new(Width, Height);

    public Vector2Data Center => new(
        Minimum.X + Width * 0.5f,
        Minimum.Y + Height * 0.5f);

    /// <summary>
    /// Determina si una posición se encuentra dentro de los límites.
    /// Los bordes se consideran parte del área.
    /// </summary>
    public bool Contains(Vector2Data value)
    {
        if (!IsValid || !value.IsFinite)
            return false;

        return value.X >= Minimum.X &&
               value.X <= Maximum.X &&
               value.Y >= Minimum.Y &&
               value.Y <= Maximum.Y;
    }

    /// <summary>
    /// Restringe una posición para que permanezca dentro de los límites.
    /// </summary>
    public Vector2Data Clamp(Vector2Data value)
    {
        if (!IsValid)
            throw new InvalidOperationException(
                "No se puede aplicar Clamp sobre BoundsData inválido.");

        if (!value.IsFinite)
            throw new ArgumentException(
                "La posición contiene valores no finitos.",
                nameof(value));

        return new Vector2Data(
            Math.Clamp(value.X, Minimum.X, Maximum.X),
            Math.Clamp(value.Y, Minimum.Y, Maximum.Y));
    }
}