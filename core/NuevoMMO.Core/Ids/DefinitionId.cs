namespace NuevoMMO.Core;

/// <summary>
/// Identificador único y estable de una definición de contenido del juego.
/// </summary>
/// <remarks>
/// Una vez asignado a una Definition, el identificador debe permanecer estable
/// aunque cambien su nombre, propiedades, balance o representación visual.
/// </remarks>
public readonly record struct DefinitionId(Guid Value)
{
    /// <summary>
    /// Identificador vacío o no asignado.
    /// </summary>
    public static DefinitionId Empty => new(Guid.Empty);

    /// <summary>
    /// Indica si el identificador no ha sido asignado.
    /// </summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>
    /// Crea un nuevo identificador único.
    /// Debe utilizarse al crear contenido nuevo, no al cargar contenido existente.
    /// </summary>
    public static DefinitionId New() => new(Guid.NewGuid());

    /// <summary>
    /// Crea un identificador a partir de un Guid existente.
    /// </summary>
    public static DefinitionId From(Guid value) => new(value);

    /// <summary>
    /// Convierte una representación textual en un DefinitionId.
    /// </summary>
    public static DefinitionId Parse(string value)
        => new(Guid.Parse(value));

    /// <summary>
    /// Intenta convertir una representación textual en un DefinitionId.
    /// </summary>
    public static bool TryParse(
        string? value,
        out DefinitionId definitionId)
    {
        if (Guid.TryParse(value, out var guid))
        {
            definitionId = new DefinitionId(guid);
            return true;
        }

        definitionId = Empty;
        return false;
    }

    /// <summary>
    /// Devuelve la representación textual estándar del identificador.
    /// </summary>
    public override string ToString() => Value.ToString();
}