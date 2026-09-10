namespace NuevoMMO.Core;

/// <summary>
/// Describe un tipo de recurso del mundo. No representa un nodo recolectable concreto del runtime.
/// </summary>
public sealed record ResourceDefinition : GameDefinition
{
    public ResourceDefinition(
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
    /// Clave visual resuelta por el cliente.
    /// </summary>
    public ContentKey VisualKey { get; }

    /// <summary>
    /// Propiedades variables que pueden describir la calidad material del recurso,
    /// por ejemplo pureza, conductividad Malden o integridad cuando el diseño lo requiera.
    /// </summary>
    public DefinitionId[] PropertyIds { get; }
}
