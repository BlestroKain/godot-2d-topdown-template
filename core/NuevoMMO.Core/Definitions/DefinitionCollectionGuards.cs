namespace NuevoMMO.Core;

internal static class DefinitionCollectionGuards
{
    public static Dictionary<string, string> CopyText(Dictionary<string, string>? source, string parameterName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return result;

        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("El diccionario contiene una clave vacía.", parameterName);
            result.Add(key.Trim(), value?.Trim() ?? string.Empty);
        }

        return result;
    }

    public static Dictionary<string, Guid> CopyGuidMap(Dictionary<string, Guid>? source, string parameterName)
    {
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return result;

        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("El diccionario contiene una clave vacía.", parameterName);
            if (value == Guid.Empty)
                throw new ArgumentException("El diccionario contiene un Guid vacío.", parameterName);
            result.Add(key.Trim(), value);
        }

        return result;
    }

    public static Dictionary<string, ContentKey> CopyContentKeys(Dictionary<string, ContentKey>? source, string parameterName)
    {
        var result = new Dictionary<string, ContentKey>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return result;

        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("El diccionario contiene una clave vacía.", parameterName);
            if (value.IsEmpty)
                throw new ArgumentException("El diccionario contiene un ContentKey vacío.", parameterName);
            result.Add(key.Trim(), value);
        }

        return result;
    }

    public static Dictionary<DefinitionId, int> CopyPositiveQuantities(
        Dictionary<DefinitionId, int>? source,
        string parameterName)
    {
        var result = source is null ? [] : new Dictionary<DefinitionId, int>(source);
        if (result.Keys.Any(static id => id.IsEmpty))
            throw new ArgumentException("El diccionario contiene un DefinitionId vacío.", parameterName);
        if (result.Values.Any(static quantity => quantity < 1))
            throw new ArgumentException("Las cantidades deben ser mayores o iguales a 1.", parameterName);
        return result;
    }

    public static Dictionary<int, long> CopyExperienceCurve(Dictionary<int, long>? source, string parameterName)
    {
        var result = source is null ? [] : new Dictionary<int, long>(source);
        if (result.Keys.Any(static level => level < 1))
            throw new ArgumentException("Los niveles de la curva deben ser mayores o iguales a 1.", parameterName);
        if (result.Values.Any(static experience => experience < 0))
            throw new ArgumentException("La experiencia no puede ser negativa.", parameterName);
        return result;
    }

    public static string[] CopyStrings(IEnumerable<string>? values)
        => values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];
}
