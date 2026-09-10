using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Copia mutable de trabajo de una MapDefinition. La UI/undo-redo modifica este documento;
/// guardar produce una nueva MapDefinition válida.
/// </summary>
public sealed class MapDocument
{
    private MapDocument() { }

    public DefinitionId Id { get; private set; }
    public ContentKey Key { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int Version { get; set; }
    public List<string> Tags { get; } = [];
    public ContentKey VisualKey { get; set; }
    public BoundsData Bounds { get; set; }
    public Vector2Data Spawn { get; set; }
    public Vector2IntData TileSize { get; set; }

    public List<MapLayerDefinition> Layers { get; } = [];
    public List<MapCollisionDefinition> Collisions { get; } = [];
    public List<MapContentPlacementDefinition> Placements { get; } = [];
    public List<MapSpawnZoneDefinition> SpawnZones { get; } = [];
    public List<MapPortalDefinition> Portals { get; } = [];
    public List<MapRegionDefinition> Regions { get; } = [];
    public List<MapLightDefinition> Lights { get; } = [];
    public MapEnvironmentDefinition Environment { get; set; } = new();
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static MapDocument FromDefinition(MapDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var document = new MapDocument
        {
            Id = definition.Id,
            Key = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            Enabled = definition.Enabled,
            Version = definition.Version,
            VisualKey = definition.VisualKey,
            Bounds = definition.Bounds,
            Spawn = definition.Spawn,
            TileSize = definition.TileSize,
            Environment = definition.Content.Environment
        };

        document.Tags.AddRange(definition.Tags);
        document.Layers.AddRange(definition.Content.Layers);
        document.Collisions.AddRange(definition.Content.Collisions);
        document.Placements.AddRange(definition.Content.Placements);
        document.SpawnZones.AddRange(definition.Content.SpawnZones);
        document.Portals.AddRange(definition.Content.Portals);
        document.Regions.AddRange(definition.Content.Regions);
        document.Lights.AddRange(definition.Content.Lights);
        foreach (var pair in definition.Content.Metadata) document.Metadata.Add(pair.Key, pair.Value);
        return document;
    }

    public MapDefinition ToDefinition()
    {
        var content = new MapContentDefinition(
            Layers.ToArray(),
            Collisions.ToArray(),
            Placements.ToArray(),
            SpawnZones.ToArray(),
            Portals.ToArray(),
            Regions.ToArray(),
            Lights.ToArray(),
            Environment,
            new Dictionary<string, string>(Metadata, StringComparer.OrdinalIgnoreCase));

        return new MapDefinition(
            Id,
            Key,
            Name,
            Description,
            Enabled,
            Version,
            Tags.ToArray(),
            VisualKey,
            Bounds,
            Spawn,
            TileSize,
            content);
    }
}
