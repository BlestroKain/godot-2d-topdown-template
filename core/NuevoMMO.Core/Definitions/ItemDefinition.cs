namespace NuevoMMO.Core;

/// <summary>
/// Describe un tipo de ítem. Las instancias concretas pueden materializar valores variables
/// para las propiedades referenciadas por esta definición.
/// </summary>
public sealed record ItemDefinition : GameDefinition
{
    public ItemDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey visualKey,
        DefinitionId[]? propertyIds = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty)
            throw new ArgumentException("VisualKey vacío.", nameof(visualKey));

        var properties = propertyIds ?? [];
        if (properties.Any(static property => property.IsEmpty))
            throw new ArgumentException("PropertyIds contiene un DefinitionId vacío.", nameof(propertyIds));

        if (properties.Distinct().Count() != properties.Length)
            throw new ArgumentException("PropertyIds contiene referencias duplicadas.", nameof(propertyIds));

        VisualKey = visualKey;
        PropertyIds = [.. properties];
    }

    /// <summary>
    /// Clave visual resuelta únicamente por el cliente.
    /// </summary>
    public ContentKey VisualKey { get; }

    /// <summary>
    /// Propiedades que una instancia de este ítem puede materializar.
    /// Cada referencia apunta a un ItemPropertyDefinition.
    /// </summary>
    public DefinitionId[] PropertyIds { get; }
}
