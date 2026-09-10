namespace NuevoMMO.Core;

public sealed record MapDefinition : GameDefinition
{
    public MapDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey visualKey,
        BoundsData bounds,
        Vector2Data spawn,
        Vector2IntData tileSize)
        : base(
            id,
            key,
            name,
            description,
            enabled,
            version,
            tags)
    {
        if (!bounds.IsValid)
            throw new ArgumentException(
                "Los límites del mapa son inválidos.",
                nameof(bounds));

        if (!spawn.IsFinite)
            throw new ArgumentException(
                "El punto de aparición contiene valores no finitos.",
                nameof(spawn));

        if (bounds.Clamp(spawn) != spawn)
            throw new ArgumentOutOfRangeException(
                nameof(spawn),
                "El punto de aparición debe estar dentro de los límites del mapa.");

        if (tileSize.X <= 0 || tileSize.Y <= 0)
            throw new ArgumentException(
                "TileSize debe contener dimensiones positivas.",
                nameof(tileSize));

        VisualKey = visualKey;
        Bounds = bounds;
        Spawn = spawn;
        TileSize = tileSize;
    }

    /// <summary>
    /// Identificador del mapa derivado de su DefinitionId.
    /// </summary>
    public MapId MapId => new(Id.Value);

    /// <summary>
    /// Clave que permite al cliente resolver la representación visual del mapa.
    /// El servidor no necesita conocer texturas, escenas ni recursos de Godot.
    /// </summary>
    public ContentKey VisualKey { get; }

    /// <summary>
    /// Límites lógicos del espacio jugable.
    /// </summary>
    public BoundsData Bounds { get; }

    /// <summary>
    /// Punto de aparición predeterminado en coordenadas continuas de mundo.
    /// </summary>
    public Vector2Data Spawn { get; }

    /// <summary>
    /// Tamaño lógico de tile utilizado por mapas, herramientas y contenido.
    /// No implica movimiento tile-by-tile.
    /// </summary>
    public Vector2IntData TileSize { get; }
}