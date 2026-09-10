namespace NuevoMMO.Core;

/// <summary>
/// Identificador legible y estable de una definición de contenido.
/// </summary>
/// <remarks>
/// Ejemplos:
/// babalum_common
/// iron_ore
/// technique.fireball
/// map.candol_fields
///
/// No debe usarse como texto visible para el jugador.
/// </remarks>
public readonly record struct ContentKey
{
    public const int MaxLength = 128;

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public ContentKey(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        value = value.Trim();

        if (value.Length is < 1 or > MaxLength)
            throw new ArgumentException(
                $"ContentKey debe tener entre 1 y {MaxLength} caracteres.",
                nameof(value));

        foreach (var character in value)
        {
            if (!IsAllowedCharacter(character))
                throw new ArgumentException(
                    $"ContentKey contiene un carácter inválido: '{character}'.",
                    nameof(value));
        }

        Value = value;
    }

    public static ContentKey Parse(string value)
        => new(value);

    public static bool TryParse(
        string? value,
        out ContentKey contentKey)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            contentKey = default;
            return false;
        }

        try
        {
            contentKey = new ContentKey(value);
            return true;
        }
        catch (ArgumentException)
        {
            contentKey = default;
            return false;
        }
    }

    public override string ToString() => Value ?? string.Empty;

    private static bool IsAllowedCharacter(char character)
    {
        return character is >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '_'
            or '-'
            or '.';
    }
}