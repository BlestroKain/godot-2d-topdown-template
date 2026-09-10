using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class RegionEditor
{
    private MapDocument? document;
    public IReadOnlyList<MapRegionDefinition> Regions => RequireDocument().Regions;

    public void Bind(MapDocument map) => document = map ?? throw new ArgumentNullException(nameof(map));

    public MapRegionDefinition Add(
        string key,
        string name,
        MapShapeDefinition area,
        string[]? tags = null,
        Dictionary<string, DefinitionId>? eventHooks = null,
        Dictionary<string, float>? parameters = null)
    {
        var region = new MapRegionDefinition(Guid.NewGuid(), key, name, area, tags, eventHooks, parameters);
        RequireDocument().Regions.Add(region);
        return region;
    }

    public bool Remove(Guid id) => RequireDocument().Regions.RemoveAll(region => region.Id == id) > 0;

    private MapDocument RequireDocument() => document ?? throw new InvalidOperationException("No hay mapa abierto.");
}
