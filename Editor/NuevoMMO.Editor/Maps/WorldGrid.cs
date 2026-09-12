using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Cuadrícula mundial de mapas, al patrón Intersect: cada mapa ocupa una celda
/// y los vecinos Norte/Sur/Oeste/Este se enlazan al crear mapas adyacentes.
/// </summary>
public static class MapWorldGrid
{
    public static MapDefinition? At(DefinitionRegistry registry, int gridX, int gridY)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.GetAll<MapDefinition>().FirstOrDefault(map => map.GridX == gridX && map.GridY == gridY);
    }

    public static bool CanCreate(DefinitionRegistry registry, int gridX, int gridY)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (At(registry, gridX, gridY) is not null) return false;
        var maps = registry.GetAll<MapDefinition>().ToArray();
        if (maps.Length == 0) return gridX == 0 && gridY == 0;
        return maps.Any(map => Math.Abs(map.GridX - gridX) + Math.Abs(map.GridY - gridY) == 1);
    }

    public static (int MinX, int MinY, int MaxX, int MaxY) VisibleBounds(DefinitionRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var maps = registry.GetAll<MapDefinition>().ToArray();
        if (maps.Length == 0) return (0, 0, 0, 0);
        var minX = maps.Min(static map => map.GridX) - 1;
        var minY = maps.Min(static map => map.GridY) - 1;
        var maxX = maps.Max(static map => map.GridX) + 1;
        var maxY = maps.Max(static map => map.GridY) + 1;
        return (minX, minY, maxX, maxY);
    }

    public static int AssignUniqueCells(DefinitionRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var maps = registry.GetAll<MapDefinition>().OrderBy(static map => map.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var used = new HashSet<(int X, int Y)>();
        var changed = 0;
        foreach (var map in maps)
        {
            var cell = (map.GridX, map.GridY);
            if (used.Add(cell)) continue;
            var x = 0;
            var y = 0;
            while (!used.Add((x, y))) x++;
            registry.Replace(map with { GridX = x, GridY = y });
            changed++;
        }

        return changed;
    }

    public static MapDefinition WithLink(MapDefinition map, Direction direction, DefinitionId neighborId)
        => direction switch
        {
            Direction.Up => map with { NorthMapId = neighborId },
            Direction.Down => map with { SouthMapId = neighborId },
            Direction.Left => map with { WestMapId = neighborId },
            Direction.Right => map with { EastMapId = neighborId },
            _ => map
        };
}
