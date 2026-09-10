namespace NuevoMMO.Core;

/// <summary>
/// Describe una propiedad variable que puede materializarse en una instancia de ítem o recurso.
/// </summary>
public sealed record ItemPropertyDefinition : GameDefinition
{
    public ItemPropertyDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        float minimum,
        float maximum)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (!float.IsFinite(minimum))
            throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Minimum debe ser finito.");
        if (!float.IsFinite(maximum))
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Maximum debe ser finito.");
        if (minimum > maximum)
            throw new ArgumentException("Minimum no puede ser mayor que Maximum.");

        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Valor mínimo permitido al materializar la propiedad.
    /// </summary>
    public float Minimum { get; }

    /// <summary>
    /// Valor máximo permitido al materializar la propiedad.
    /// </summary>
    public float Maximum { get; }
}
