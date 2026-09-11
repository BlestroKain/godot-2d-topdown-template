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
        Vector2IntData tileSize,
        MapContentDefinition? content = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));
        if (!bounds.IsValid) throw new ArgumentException("Los límites del mapa son inválidos.", nameof(bounds));
        if (!spawn.IsFinite) throw new ArgumentException("El punto de aparición contiene valores no finitos.", nameof(spawn));
        if (bounds.Clamp(spawn) != spawn)
            throw new ArgumentOutOfRangeException(nameof(spawn), "El punto de aparición debe estar dentro de los límites del mapa.");
        if (tileSize.X <= 0 || tileSize.Y <= 0)
            throw new ArgumentException("TileSize debe contener dimensiones positivas.", nameof(tileSize));

        VisualKey = visualKey;
        Bounds = bounds;
        Spawn = spawn;
        TileSize = tileSize;
        Content = content ?? new MapContentDefinition();

        foreach (var placement in Content.Placements)
            EnsureInside(placement.Position, nameof(content), "placement");
        foreach (var light in Content.Lights)
            EnsureInside(light.Position, nameof(content), "light");
        foreach (var zone in Content.SpawnZones)
            EnsureInside(zone.Area.Center, nameof(content), "spawn zone");
        foreach (var region in Content.Regions)
            EnsureInside(region.Area.Center, nameof(content), "region");
        foreach (var portal in Content.Portals)
            EnsureInside(portal.TriggerArea.Center, nameof(content), "portal");
    }

    public MapId MapId => new(Id.Value);

    /// <summary>
    /// Clave de representación/previsualización resuelta por el cliente/editor.
    /// El servidor no carga escenas ni recursos Godot.
    /// </summary>
    public ContentKey VisualKey { get; }

    public BoundsData Bounds { get; }
    public Vector2Data Spawn { get; }

    /// <summary>
    /// Tamaño lógico del tile utilizado por pintura y tooling. No implica movimiento tile-by-tile
    /// ni convierte la rejilla en autoridad de colisión.
    /// </summary>
    public Vector2IntData TileSize { get; }

    /// <summary>
    /// Documento editable del mapa: capas visuales, geometría de colisión continua,
    /// placements, zonas de spawn, portales, regiones, luces y ambiente.
    /// </summary>
    public MapContentDefinition Content { get; }

    private void EnsureInside(Vector2Data position, string parameterName, string kind)
    {
        if (Bounds.Clamp(position) != position)
            throw new ArgumentOutOfRangeException(parameterName, $"El {kind} está fuera de los límites del mapa.");
    }
}
