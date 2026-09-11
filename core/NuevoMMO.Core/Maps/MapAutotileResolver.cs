namespace NuevoMMO.Core;

public enum MapTileRenderState : byte
{
    Normal = 1,
    Autotile = 2
}

/// <summary>
/// Resultado derivado de una celda. No se serializa en MapDefinition: Client/Editor lo cachean.
/// QuarterSourcePixels contiene NW, NE, SW y SE en ese orden cuando RenderState=Autotile.
/// </summary>
public sealed record MapAutotileRenderData
{
    public MapAutotileRenderData(
        MapTileRenderState renderState,
        Vector2IntData[]? quarterSourcePixels = null)
    {
        var quarters = quarterSourcePixels?.ToArray() ?? [];
        if (renderState == MapTileRenderState.Autotile && quarters.Length != 4)
            throw new ArgumentException("Un autotile resuelto requiere exactamente cuatro quarter-tiles.", nameof(quarterSourcePixels));
        if (renderState == MapTileRenderState.Normal && quarters.Length != 0)
            throw new ArgumentException("Un tile normal no requiere quarter-tiles.", nameof(quarterSourcePixels));

        RenderState = renderState;
        QuarterSourcePixels = quarters;
    }

    public MapTileRenderState RenderState { get; }
    public Vector2IntData[] QuarterSourcePixels { get; }

    /// <summary>
    /// Offset de frame compatible con Intersect. Autotile/XP avanzan horizontalmente;
    /// waterfall usa tres posiciones verticales alrededor de la celda base; cliff usa la fila superior.
    /// </summary>
    public static Vector2IntData FrameOffset(
        MapAutotileMode mode,
        int autotileFrame,
        int waterfallFrame,
        Vector2IntData tileSize)
        => mode switch
        {
            MapAutotileMode.Waterfall => new(0, (waterfallFrame - 1) * tileSize.Y),
            MapAutotileMode.Animated => new(autotileFrame * tileSize.X * 2, 0),
            MapAutotileMode.AnimatedXp => new(autotileFrame * tileSize.X * 3, 0),
            MapAutotileMode.Cliff => new(0, -tileSize.Y),
            _ => default
        };
}

/// <summary>
/// Resolver puro de autotiles portado de MapAutotiles de Intersect.
/// El mapa conserva únicamente tiles base + modo; este servicio deriva los quarter-tiles de render.
/// </summary>
public sealed class MapAutotileResolver
{
    private enum Situation : byte
    {
        Inner = 1,
        Outer = 2,
        Horizontal = 3,
        Vertical = 4,
        Fill = 5
    }

    private enum XpSituation : byte
    {
        Fill = 1,
        Inner = 2,
        NorthWest = 3,
        North = 4,
        NorthEast = 5,
        East = 6,
        SouthEast = 7,
        South = 8,
        SouthWest = 9,
        West = 10
    }

    private readonly int width;
    private readonly int height;
    private readonly Vector2IntData tileSize;
    private readonly IReadOnlyDictionary<Vector2IntData, MapTilePlacementDefinition> tiles;
    private readonly Func<Vector2IntData, MapTilePlacementDefinition?>? outsideTileLookup;

    public MapAutotileResolver(
        MapLayerDefinition layer,
        int width,
        int height,
        Vector2IntData tileSize,
        Func<Vector2IntData, MapTilePlacementDefinition?>? outsideTileLookup = null)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
        if (tileSize.X <= 0 || tileSize.Y <= 0 || (tileSize.X & 1) != 0 || (tileSize.Y & 1) != 0)
            throw new ArgumentException("TileSize debe ser positivo y par.", nameof(tileSize));

        this.width = width;
        this.height = height;
        this.tileSize = tileSize;
        this.outsideTileLookup = outsideTileLookup;
        tiles = layer.Tiles.ToDictionary(static tile => tile.Cell);
    }

    public static Dictionary<Vector2IntData, MapAutotileRenderData> ResolveLayer(
        MapDefinition map,
        MapLayerDefinition layer,
        Func<Vector2IntData, MapTilePlacementDefinition?>? outsideTileLookup = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        var width = Math.Max(1, (int)Math.Ceiling(map.Bounds.Width / map.TileSize.X));
        var height = Math.Max(1, (int)Math.Ceiling(map.Bounds.Height / map.TileSize.Y));
        return new MapAutotileResolver(layer, width, height, map.TileSize, outsideTileLookup).ResolveAll();
    }

    public Dictionary<Vector2IntData, MapAutotileRenderData> ResolveAll()
    {
        var result = new Dictionary<Vector2IntData, MapAutotileRenderData>();
        foreach (var tile in tiles.Values)
            result[tile.Cell] = Resolve(tile.Cell);
        return result;
    }

    public MapAutotileRenderData Resolve(Vector2IntData cell)
    {
        var tile = GetTile(cell) ?? throw new KeyNotFoundException($"No existe tile en {cell}.");
        if (tile.Autotile is MapAutotileMode.None or MapAutotileMode.Fake)
            return new MapAutotileRenderData(MapTileRenderState.Normal);

        var letters = tile.Autotile switch
        {
            MapAutotileMode.Normal or MapAutotileMode.Animated => ResolveNormal(cell),
            MapAutotileMode.Cliff => ResolveCliff(cell),
            MapAutotileMode.Waterfall => ResolveWaterfall(cell),
            MapAutotileMode.Xp or MapAutotileMode.AnimatedXp => ResolveXp(cell),
            _ => throw new ArgumentOutOfRangeException(nameof(tile.Autotile), tile.Autotile, "Modo de autotile desconocido.")
        };

        var basePixel = tile.AtlasPixelOrigin(tileSize);
        var quarters = new Vector2IntData[4];
        for (var index = 0; index < 4; index++)
        {
            var relative = tile.Autotile is MapAutotileMode.Xp or MapAutotileMode.AnimatedXp
                ? XpTemplate(letters[index])
                : VxTemplate(letters[index]);
            quarters[index] = new Vector2IntData(basePixel.X + relative.X, basePixel.Y + relative.Y);
        }

        return new MapAutotileRenderData(MapTileRenderState.Autotile, quarters);
    }

    /// <summary>
    /// Recalcula la misma vecindad 3x3 que Intersect al pintar un tile.
    /// Si hay cliffs implicados, se incluyen todos los cliffs de la capa porque su altura puede propagarse verticalmente.
    /// </summary>
    public IReadOnlyCollection<Vector2IntData> AffectedCells(Vector2IntData changedCell)
    {
        var affected = new HashSet<Vector2IntData>();
        if (IsInside(changedCell)) affected.Add(changedCell);

        for (var x = changedCell.X - 1; x <= changedCell.X + 1; x++)
        for (var y = changedCell.Y - 1; y <= changedCell.Y + 1; y++)
        {
            var cell = new Vector2IntData(x, y);
            if (IsInside(cell) && tiles.ContainsKey(cell)) affected.Add(cell);
        }

        if (LayerHasCliff)
        {
            foreach (var tile in tiles.Values)
                if (tile.Autotile == MapAutotileMode.Cliff) affected.Add(tile.Cell);
        }

        return affected;
    }

    public bool RequiresFullRefresh => LayerHasCliff;

    private bool LayerHasCliff => tiles.Values.Any(static tile => tile.Autotile == MapAutotileMode.Cliff);

    private char[] ResolveNormal(Vector2IntData c)
    {
        var nw = SituationFor(
            CheckMatch(c, new(c.X - 1, c.Y - 1)),
            CheckMatch(c, new(c.X, c.Y - 1)),
            CheckMatch(c, new(c.X - 1, c.Y)),
            northwest: true);
        var ne = SituationFor(
            CheckMatch(c, new(c.X + 1, c.Y - 1)),
            CheckMatch(c, new(c.X, c.Y - 1)),
            CheckMatch(c, new(c.X + 1, c.Y)),
            northwest: true);
        var sw = SituationFor(
            CheckMatch(c, new(c.X - 1, c.Y + 1)),
            CheckMatch(c, new(c.X, c.Y + 1)),
            CheckMatch(c, new(c.X - 1, c.Y)),
            northwest: true);
        var se = SituationFor(
            CheckMatch(c, new(c.X + 1, c.Y + 1)),
            CheckMatch(c, new(c.X, c.Y + 1)),
            CheckMatch(c, new(c.X + 1, c.Y)),
            northwest: true);

        return
        [
            NormalLetter(0, nw),
            NormalLetter(1, ne),
            NormalLetter(2, sw),
            NormalLetter(3, se)
        ];
    }

    private static Situation SituationFor(bool diagonal, bool vertical, bool horizontal, bool northwest)
    {
        _ = northwest;
        if (!vertical && !horizontal) return Situation.Inner;
        if (!vertical && horizontal) return Situation.Horizontal;
        if (vertical && !horizontal) return Situation.Vertical;
        if (!diagonal) return Situation.Outer;
        return Situation.Fill;
    }

    private static char NormalLetter(int quarter, Situation situation)
        => (quarter, situation) switch
        {
            (0, Situation.Inner) => 'e', (0, Situation.Outer) => 'a', (0, Situation.Horizontal) => 'i',
            (0, Situation.Vertical) => 'm', (0, Situation.Fill) => 'q',
            (1, Situation.Inner) => 'j', (1, Situation.Outer) => 'b', (1, Situation.Horizontal) => 'f',
            (1, Situation.Vertical) => 'r', (1, Situation.Fill) => 'n',
            (2, Situation.Inner) => 'o', (2, Situation.Outer) => 'c', (2, Situation.Horizontal) => 's',
            (2, Situation.Vertical) => 'g', (2, Situation.Fill) => 'k',
            (3, Situation.Inner) => 't', (3, Situation.Outer) => 'd', (3, Situation.Horizontal) => 'p',
            (3, Situation.Vertical) => 'l', (3, Situation.Fill) => 'h',
            _ => throw new InvalidOperationException("Situación de autotile inválida.")
        };

    private char[] ResolveWaterfall(Vector2IntData c)
    {
        var left = CheckMatch(c, new(c.X - 1, c.Y));
        var right = CheckMatch(c, new(c.X + 1, c.Y));
        return
        [
            left ? 'i' : 'e',
            right ? 'f' : 'j',
            left ? 'k' : 'g',
            right ? 'h' : 'l'
        ];
    }

    private char[] ResolveCliff(Vector2IntData c)
    {
        var cliffHeight = CalculateCliffHeight(c, out var cliffStart);

        var leftStart = 0;
        var leftHeight = 0;
        if (CheckMatch(c, new(c.X - 1, c.Y)))
            leftHeight = CalculateCliffHeight(new(c.X - 1, c.Y), out leftStart);

        var rightStart = 0;
        var rightHeight = 0;
        if (CheckMatch(c, new(c.X + 1, c.Y)))
            rightHeight = CalculateCliffHeight(new(c.X + 1, c.Y), out rightStart);

        var assumeInteriorEast = CheckMatch(c, new(c.X + 1, cliffStart)) &&
                                 !CheckMatch(c, new(c.X + 1, cliffStart - 1));
        var assumeInteriorWest = CheckMatch(c, new(c.X - 1, cliffStart)) &&
                                 !CheckMatch(c, new(c.X - 1, cliffStart - 1));

        var rangeHeight = cliffHeight;
        var x = c.X - 1;
        while (x > -width && CheckMatch(c, new(x, cliffStart)))
        {
            var h = CalculateCliffHeight(new(x, cliffStart), out var start);
            if (start != cliffStart) break;
            rangeHeight = Math.Max(rangeHeight, h);
            x--;
        }

        x = c.X + 1;
        while (x < width * 2 && CheckMatch(c, new(x, cliffStart)))
        {
            var h = CalculateCliffHeight(new(x, cliffStart), out var start);
            if (start != cliffStart) break;
            rangeHeight = Math.Max(rangeHeight, h);
            x++;
        }

        var drawBottom = !((assumeInteriorEast && rangeHeight > cliffHeight) ||
                           (assumeInteriorWest && rangeHeight > cliffHeight));
        if ((assumeInteriorEast || assumeInteriorWest) && cliffHeight == 1 && cliffStart != c.Y)
            drawBottom = true;

        return
        [
            CliffNorthWest(c, cliffStart, cliffHeight, leftStart, leftHeight, assumeInteriorWest),
            CliffNorthEast(c, cliffStart, cliffHeight, rightStart, rightHeight, assumeInteriorEast),
            CliffSouthWest(c, cliffStart, cliffHeight, leftStart, leftHeight, assumeInteriorWest, drawBottom),
            CliffSouthEast(c, cliffStart, cliffHeight, rightStart, rightHeight, assumeInteriorEast, drawBottom)
        ];
    }

    private char CliffNorthWest(
        Vector2IntData c,
        int cliffStart,
        int cliffHeight,
        int adjacentStart,
        int adjacentHeight,
        bool assumeInterior)
    {
        var north = CheckMatch(c, new(c.X, c.Y - 1));
        var west = SideInside(cliffStart, cliffHeight, adjacentStart, adjacentHeight);
        var situation = !north && west ? Situation.Horizontal :
                        north && !west ? Situation.Vertical :
                        north && west ? Situation.Fill : Situation.Inner;
        if (situation == Situation.Vertical && assumeInterior) situation = Situation.Fill;
        return situation switch
        {
            Situation.Inner => 'e', Situation.Horizontal => 'i', Situation.Vertical => 'm', Situation.Fill => 'q',
            _ => 'e'
        };
    }

    private char CliffNorthEast(
        Vector2IntData c,
        int cliffStart,
        int cliffHeight,
        int adjacentStart,
        int adjacentHeight,
        bool assumeInterior)
    {
        var north = CheckMatch(c, new(c.X, c.Y - 1));
        var east = SideInside(cliffStart, cliffHeight, adjacentStart, adjacentHeight);
        var situation = !north && east ? Situation.Horizontal :
                        north && !east ? Situation.Vertical :
                        north && east ? Situation.Fill : Situation.Inner;
        if (situation == Situation.Vertical && assumeInterior) situation = Situation.Fill;
        return situation switch
        {
            Situation.Inner => 'j', Situation.Horizontal => 'f', Situation.Vertical => 'r', Situation.Fill => 'n',
            _ => 'j'
        };
    }

    private char CliffSouthWest(
        Vector2IntData c,
        int cliffStart,
        int cliffHeight,
        int adjacentStart,
        int adjacentHeight,
        bool assumeInterior,
        bool drawBottom)
    {
        var west = SideInside(cliffStart, cliffHeight, adjacentStart, adjacentHeight);
        var south = CheckMatch(c, new(c.X, c.Y + 1)) || !drawBottom;
        var situation = !west && south ? Situation.Vertical :
                        west && south ? Situation.Fill :
                        west && !south ? Situation.Horizontal : Situation.Inner;
        if (situation == Situation.Vertical && assumeInterior) situation = Situation.Fill;
        if (situation == Situation.Inner && assumeInterior) situation = Situation.Horizontal;
        return situation switch
        {
            Situation.Inner => 'o', Situation.Horizontal => 's', Situation.Vertical => 'g', Situation.Fill => 'k',
            _ => 'o'
        };
    }

    private char CliffSouthEast(
        Vector2IntData c,
        int cliffStart,
        int cliffHeight,
        int adjacentStart,
        int adjacentHeight,
        bool assumeInterior,
        bool drawBottom)
    {
        var east = SideInside(cliffStart, cliffHeight, adjacentStart, adjacentHeight);
        var south = CheckMatch(c, new(c.X, c.Y + 1)) || !drawBottom;
        var situation = south && !east ? Situation.Vertical :
                        south && east ? Situation.Fill :
                        !south && east ? Situation.Horizontal : Situation.Inner;
        if (situation == Situation.Vertical && assumeInterior) situation = Situation.Fill;
        if (situation == Situation.Inner && assumeInterior) situation = Situation.Horizontal;
        return situation switch
        {
            Situation.Inner => 't', Situation.Horizontal => 'p', Situation.Vertical => 'l', Situation.Fill => 'h',
            _ => 't'
        };
    }

    private static bool SideInside(int cliffStart, int cliffHeight, int adjacentStart, int adjacentHeight)
        => adjacentHeight > 0 &&
           (cliffStart == adjacentStart ||
            (cliffStart > adjacentStart && cliffStart + cliffHeight <= adjacentStart + adjacentHeight));

    private int CalculateCliffHeight(Vector2IntData sourceCell, out int cliffStart)
    {
        cliffStart = sourceCell.Y;
        var source = GetTile(sourceCell);
        if (source?.Autotile != MapAutotileMode.Cliff) return 0;

        var result = 1;
        var y = sourceCell.Y - 1;
        while (y > -height)
        {
            if (!CheckMatch(sourceCell, new(sourceCell.X, y))) break;
            result++;
            cliffStart--;
            y--;
        }

        y = sourceCell.Y + 1;
        while (y < height * 2)
        {
            if (!CheckMatch(sourceCell, new(sourceCell.X, y))) break;
            result++;
            y++;
        }

        return result;
    }

    private char[] ResolveXp(Vector2IntData c)
    {
        var match = new bool[3, 3];
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
            match[dx + 1, dy + 1] = CheckMatch(c, new(c.X + dx, c.Y + dy));

        return
        [
            XpLetter(0, XpNorthWest(match)),
            XpLetter(1, XpNorthEast(match)),
            XpLetter(2, XpSouthWest(match)),
            XpLetter(3, XpSouthEast(match))
        ];
    }

    private static XpSituation XpNorthWest(bool[,] m)
    {
        var west = m[0, 1]; var north = m[1, 0]; var east = m[2, 1]; var south = m[1, 2]; var nw = m[0, 0];
        var situation = XpSituation.Fill;
        if (west && !south) situation = east ? XpSituation.South : XpSituation.SouthEast;
        if (west && !north) situation = east ? XpSituation.North : XpSituation.NorthEast;
        if (!east && north) situation = south ? XpSituation.East : XpSituation.SouthEast;
        if (!west && north) situation = south ? XpSituation.West : XpSituation.SouthWest;
        if (!west && !north) situation = XpSituation.NorthWest;
        if (west && north && !nw) situation = XpSituation.Inner;
        if (nw && north && west) situation = XpSituation.Fill;
        return situation;
    }

    private static XpSituation XpNorthEast(bool[,] m)
    {
        var west = m[0, 1]; var north = m[1, 0]; var east = m[2, 1]; var south = m[1, 2]; var ne = m[2, 0];
        var situation = XpSituation.Fill;
        if (west && !south) situation = east ? XpSituation.South : XpSituation.SouthEast;
        if (west && !north) situation = east ? XpSituation.North : XpSituation.NorthEast;
        if (!west && north) situation = south ? XpSituation.West : XpSituation.SouthWest;
        if (!east && north) situation = south ? XpSituation.East : XpSituation.SouthEast;
        if (!west && !north) situation = XpSituation.NorthWest;
        if (!north && !east) situation = XpSituation.NorthEast;
        if (east && north && !ne) situation = XpSituation.Inner;
        if (north && ne && east) situation = XpSituation.Fill;
        return situation;
    }

    private static XpSituation XpSouthWest(bool[,] m)
    {
        var west = m[0, 1]; var north = m[1, 0]; var east = m[2, 1]; var south = m[1, 2]; var sw = m[0, 2];
        var situation = XpSituation.Fill;
        if (west && !north) situation = east ? XpSituation.North : XpSituation.NorthEast;
        if (west && !south) situation = east ? XpSituation.South : XpSituation.SouthEast;
        if (!east && north) situation = south ? XpSituation.East : XpSituation.SouthEast;
        if (!west && north) situation = south ? XpSituation.West : XpSituation.SouthWest;
        if (!west && !north) situation = XpSituation.NorthWest;
        if (!west && !south) situation = XpSituation.SouthWest;
        if (west && south && !sw) situation = XpSituation.Inner;
        if (west && sw && south) situation = XpSituation.Fill;
        return situation;
    }

    private static XpSituation XpSouthEast(bool[,] m)
    {
        var west = m[0, 1]; var north = m[1, 0]; var east = m[2, 1]; var south = m[1, 2]; var se = m[2, 2];
        var situation = XpSituation.Fill;
        if (west && !north) situation = east ? XpSituation.North : XpSituation.NorthEast;
        if (west && !south) situation = east ? XpSituation.South : XpSituation.SouthEast;
        if (!west && north) situation = south ? XpSituation.West : XpSituation.SouthWest;
        if (!east && north) situation = south ? XpSituation.East : XpSituation.SouthEast;
        if (!west && !north) situation = XpSituation.NorthWest;
        if (!west && !south) situation = XpSituation.SouthWest;
        if (!north && !east) situation = XpSituation.NorthEast;
        if (!south && !east) situation = XpSituation.SouthEast;
        if (east && south && !se) situation = XpSituation.Inner;
        if (east && se && south) situation = XpSituation.Fill;
        return situation;
    }

    private static char XpLetter(int quarter, XpSituation situation)
        => (quarter, situation) switch
        {
            (0, XpSituation.Inner) => 'a', (0, XpSituation.Fill) => 'A', (0, XpSituation.NorthWest) => 'e',
            (0, XpSituation.North) => 'E', (0, XpSituation.NorthEast) => 'i', (0, XpSituation.East) => 'I',
            (0, XpSituation.SouthEast) => 'q', (0, XpSituation.South) => 'Q', (0, XpSituation.SouthWest) => 'm',
            (0, XpSituation.West) => 'M',
            (1, XpSituation.Inner) => 'b', (1, XpSituation.Fill) => 'B', (1, XpSituation.NorthWest) => 'f',
            (1, XpSituation.North) => 'F', (1, XpSituation.NorthEast) => 'j', (1, XpSituation.East) => 'J',
            (1, XpSituation.SouthEast) => 'r', (1, XpSituation.South) => 'R', (1, XpSituation.SouthWest) => 'n',
            (1, XpSituation.West) => 'N',
            (2, XpSituation.Inner) => 'c', (2, XpSituation.Fill) => 'C', (2, XpSituation.NorthWest) => 'g',
            (2, XpSituation.North) => 'G', (2, XpSituation.NorthEast) => 'k', (2, XpSituation.East) => 'K',
            (2, XpSituation.SouthEast) => 's', (2, XpSituation.South) => 'S', (2, XpSituation.SouthWest) => 'o',
            (2, XpSituation.West) => 'O',
            (3, XpSituation.Inner) => 'd', (3, XpSituation.Fill) => 'D', (3, XpSituation.NorthWest) => 'h',
            (3, XpSituation.North) => 'H', (3, XpSituation.NorthEast) => 'l', (3, XpSituation.East) => 'L',
            (3, XpSituation.SouthEast) => 't', (3, XpSituation.South) => 'T', (3, XpSituation.SouthWest) => 'p',
            (3, XpSituation.West) => 'P',
            _ => throw new InvalidOperationException("Situación XP inválida.")
        };

    private bool CheckMatch(Vector2IntData sourceCell, Vector2IntData targetCell)
    {
        var source = GetTile(sourceCell);
        if (source is null) return true;

        MapTilePlacementDefinition? target;
        if (!IsInside(targetCell) && outsideTileLookup is null)
            return true;
        target = GetTile(targetCell);

        if (target is null) return false;
        if (target.Autotile == MapAutotileMode.Fake) return true;
        if (target.Autotile == MapAutotileMode.None) return false;
        return source.TilesetKey == target.TilesetKey &&
               source.AtlasCell == target.AtlasCell &&
               source.Alternative == target.Alternative;
    }

    private MapTilePlacementDefinition? GetTile(Vector2IntData cell)
    {
        if (IsInside(cell)) return tiles.GetValueOrDefault(cell);
        return outsideTileLookup?.Invoke(cell);
    }

    private bool IsInside(Vector2IntData cell)
        => cell.X >= 0 && cell.X < width && cell.Y >= 0 && cell.Y < height;

    private Vector2IntData VxTemplate(char letter)
    {
        var w = tileSize.X;
        var h = tileSize.Y;
        var hw = w / 2;
        var hh = h / 2;
        return letter switch
        {
            'a' => new(w, 0), 'b' => new(2 * w - hw, 0), 'c' => new(w, hh), 'd' => new(2 * w - hw, hh),
            'e' => new(0, h), 'f' => new(hw, h), 'g' => new(0, 2 * h - hh), 'h' => new(hw, 2 * h - hh),
            'i' => new(w, h), 'j' => new(2 * w - hw, h), 'k' => new(w, 2 * h - hh), 'l' => new(2 * w - hw, 2 * h - hh),
            'm' => new(0, 2 * h), 'n' => new(hw, 2 * h), 'o' => new(0, 2 * h + hh), 'p' => new(hw, 2 * h + hh),
            'q' => new(w, 2 * h), 'r' => new(2 * w - hw, 2 * h), 's' => new(w, 2 * h + hh), 't' => new(2 * w - hw, 2 * h + hh),
            _ => throw new ArgumentOutOfRangeException(nameof(letter), letter, "Letra VX inválida.")
        };
    }

    private Vector2IntData XpTemplate(char letter)
    {
        var w = tileSize.X;
        var h = tileSize.Y;
        var hw = w / 2;
        var hh = h / 2;
        return letter switch
        {
            'a' => new(2 * w, 0), 'b' => new(2 * w + hw, 0), 'c' => new(2 * w, hh), 'd' => new(2 * w + hw, hh),
            'e' => new(0, h), 'f' => new(hw, h), 'g' => new(0, h + hh), 'h' => new(hw, h + hh),
            'i' => new(2 * w, h), 'j' => new(2 * w + hw, h), 'k' => new(2 * w, h + hh), 'l' => new(2 * w + hw, h + hh),
            'm' => new(0, 3 * h), 'n' => new(hw, 3 * h), 'o' => new(0, 3 * h + hh), 'p' => new(hw, 3 * h + hh),
            'q' => new(2 * w, 3 * h), 'r' => new(2 * w + hw, 3 * h), 's' => new(2 * w, 3 * h + hh), 't' => new(2 * w + hw, 3 * h + hh),
            'A' => new(w, 2 * h), 'B' => new(w + hw, 2 * h), 'C' => new(w, 2 * h + hh), 'D' => new(w + hw, 2 * h + hh),
            'E' => new(w, h), 'F' => new(w + hw, h), 'G' => new(w, h + hh), 'H' => new(w + hw, h + hh),
            'I' => new(2 * w, 2 * h), 'J' => new(2 * w + hw, 2 * h), 'K' => new(2 * w, 2 * h + hh), 'L' => new(2 * w + hw, 2 * h + hh),
            'M' => new(0, 2 * h), 'N' => new(hw, 2 * h), 'O' => new(0, 2 * h + hh), 'P' => new(hw, 2 * h + hh),
            'Q' => new(w, 3 * h), 'R' => new(w + hw, 3 * h), 'S' => new(w, 3 * h + hh), 'T' => new(w + hw, 3 * h + hh),
            _ => throw new ArgumentOutOfRangeException(nameof(letter), letter, "Letra XP inválida.")
        };
    }
}
