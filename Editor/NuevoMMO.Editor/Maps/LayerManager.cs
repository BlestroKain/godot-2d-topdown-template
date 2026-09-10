using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class LayerManager
{
    private MapDocument? document;
    private string? activeLayerKey;

    public IReadOnlyList<MapLayerDefinition> Layers => RequireDocument().Layers
        .OrderBy(static layer => layer.Band)
        .ThenBy(static layer => layer.Order)
        .ToArray();

    public string? ActiveLayerKey => activeLayerKey;

    public void Bind(MapDocument map)
    {
        document = map ?? throw new ArgumentNullException(nameof(map));
        activeLayerKey = Layers.FirstOrDefault()?.Key;
    }

    public MapLayerDefinition Add(
        string key,
        MapLayerBand band = MapLayerBand.Lower,
        int? order = null)
    {
        var document = RequireDocument();
        if (MapLayerDefaults.IsReservedEditorLayerName(key))
            throw new ArgumentException($"'{key}' es un nombre reservado del editor.", nameof(key));
        if (document.Layers.Any(layer => string.Equals(layer.Key, key, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Ya existe la capa '{key}'.");

        var layer = new MapLayerDefinition(key, order ?? NextOrder(document, band), band: band);
        document.Layers.Add(layer);
        activeLayerKey ??= layer.Key;
        return layer;
    }

    public void AddIntersectDefaultsIfEmpty()
    {
        var document = RequireDocument();
        if (document.Layers.Count != 0) return;
        document.Layers.AddRange(MapLayerDefaults.Create());
        activeLayerKey = document.Layers[0].Key;
    }

    public void Select(string key)
    {
        var layer = RequireLayer(key);
        activeLayerKey = layer.Key;
    }

    public MapLayerDefinition Active()
    {
        if (activeLayerKey is null) throw new InvalidOperationException("No hay una capa activa.");
        return RequireLayer(activeLayerKey);
    }

    public void Replace(MapLayerDefinition layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        var document = RequireDocument();
        var index = document.Layers.FindIndex(
            existing => string.Equals(existing.Key, layer.Key, StringComparison.OrdinalIgnoreCase));
        if (index < 0) throw new KeyNotFoundException($"No existe la capa '{layer.Key}'.");
        document.Layers[index] = layer;
    }

    public bool Remove(string key)
    {
        var document = RequireDocument();
        var removed = document.Layers.RemoveAll(
            layer => string.Equals(layer.Key, key, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed && string.Equals(activeLayerKey, key, StringComparison.OrdinalIgnoreCase))
            activeLayerKey = Layers.FirstOrDefault()?.Key;
        return removed;
    }

    public void Move(string key, int newOrder, MapLayerBand? newBand = null)
    {
        var layer = RequireLayer(key);
        Replace(new MapLayerDefinition(
            layer.Key,
            newOrder,
            layer.Tiles,
            layer.Visible,
            layer.ParallaxFactor,
            layer.Parameters,
            newBand ?? layer.Band));
    }

    private MapLayerDefinition RequireLayer(string key)
        => RequireDocument().Layers.FirstOrDefault(
               layer => string.Equals(layer.Key, key, StringComparison.OrdinalIgnoreCase))
           ?? throw new KeyNotFoundException($"No existe la capa '{key}'.");

    private MapDocument RequireDocument() => document ?? throw new InvalidOperationException("No hay mapa abierto.");

    private static int NextOrder(MapDocument document, MapLayerBand band)
    {
        var sameBand = document.Layers.Where(layer => layer.Band == band).ToArray();
        return sameBand.Length == 0 ? 0 : sameBand.Max(static layer => layer.Order) + 1;
    }
}
