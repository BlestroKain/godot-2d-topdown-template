namespace NuevoMMO.Editor;

public sealed class MapEditor
{
    public MapCanvas Canvas { get; } = new();
    public TilePalette Palette { get; } = new();
    public LayerManager Layers { get; } = new();
    public CollisionEditor Collision { get; } = new();
    public SpawnEditor Spawns { get; } = new();
    public ResourceNodeEditor Resources { get; } = new();
    public PortalEditor Portals { get; } = new();
    public RegionEditor Regions { get; } = new();
    public EnvironmentEditor Environment { get; } = new();
    public MapValidator Validator { get; } = new();
}
