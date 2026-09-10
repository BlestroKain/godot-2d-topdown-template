namespace NuevoMMO.Core;

public abstract record GameDefinition
{
    protected GameDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description = null,
        bool enabled = true,
        int version = 1,
        string[]? tags = null)
    {
        if (id.IsEmpty)
            throw new ArgumentException("DefinitionId vacío.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nombre de presentación vacío.", nameof(name));

        if (version < 1)
            throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                "La versión debe ser mayor o igual a 1.");

        Id = id;
        Key = key;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Enabled = enabled;
        Version = version;

        Tags = tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];
    }

    /// <summary>
    /// Identificador técnico único e inmutable de la definición.
    /// </summary>
    public DefinitionId Id { get; }

    /// <summary>
    /// Clave legible y estable utilizada por herramientas y contenido.
    /// </summary>
    public ContentKey Key { get; }

    /// <summary>
    /// Nombre de presentación de la definición.
    /// No debe utilizarse como identidad.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Descripción del contenido.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Indica si la definición puede utilizarse actualmente.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Versión del contenido.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Etiquetas auxiliares para búsqueda, filtrado y tooling.
    /// </summary>
    public string[] Tags { get; }
}