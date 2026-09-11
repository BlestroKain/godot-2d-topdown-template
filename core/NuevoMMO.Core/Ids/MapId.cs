namespace NuevoMMO.Core;

/// <summary>
/// Identificador tipado de una definición de mapa.
/// Comparte identidad con el DefinitionId correspondiente.
/// </summary>
public readonly record struct MapId(Guid Value)
{
    public static MapId Empty => new(Guid.Empty);

    public bool IsEmpty => Value == Guid.Empty;

    public static MapId From(DefinitionId definitionId)
    {
        if (definitionId.IsEmpty)
            throw new ArgumentException(
                "DefinitionId vacío.",
                nameof(definitionId));

        return new MapId(definitionId.Value);
    }

    public override string ToString()
        => Value.ToString();
}