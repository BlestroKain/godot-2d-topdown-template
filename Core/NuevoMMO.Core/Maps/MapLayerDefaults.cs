namespace NuevoMMO.Core;

/// <summary>
/// Capas iniciales compatibles con el flujo clásico de Intersect.
/// Se pueden añadir, quitar o reordenar capas desde el Editor; estos son solo defaults.
/// </summary>
public static class MapLayerDefaults
{
    public static MapLayerDefinition[] Create()
        =>
        [
            new MapLayerDefinition("Ground", 0, band: MapLayerBand.Lower),
            new MapLayerDefinition("Mask 1", 1, band: MapLayerBand.Lower),
            new MapLayerDefinition("Mask 2", 2, band: MapLayerBand.Lower),
            new MapLayerDefinition("Fringe 1", 3, band: MapLayerBand.Middle),
            new MapLayerDefinition("Fringe 2", 4, band: MapLayerBand.Upper)
        ];

    public static bool IsReservedEditorLayerName(string key)
        => key.Equals("Attributes", StringComparison.OrdinalIgnoreCase) ||
           key.Equals("Npcs", StringComparison.OrdinalIgnoreCase) ||
           key.Equals("Lights", StringComparison.OrdinalIgnoreCase) ||
           key.Equals("Events", StringComparison.OrdinalIgnoreCase);
}
