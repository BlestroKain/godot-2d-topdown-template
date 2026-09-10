using System.Runtime.CompilerServices;
using NuevoMMO.Core;

internal static class MapAutotileVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyModeCompatibility();
        VerifyDefaultLayers();
        VerifyNormalAutotilePatterns();
        VerifyFakeAndWaterfall();
        VerifyCliffAndXp();
        VerifyNeighborMapsAndAffectedCells();
        VerifyAlternativeAtlasOffset();
        VerifyAnimationOffsets();
        VerifyTilesetPackageRoundtrip();
    }

    private static void VerifyModeCompatibility()
    {
        Expect((byte)MapAutotileMode.None == 0, "Autotile None=0");
        Expect((byte)MapAutotileMode.Normal == 1, "Autotile Normal=1");
        Expect((byte)MapAutotileMode.Fake == 2, "Autotile Fake=2");
        Expect((byte)MapAutotileMode.Animated == 3, "Autotile Animated=3");
        Expect((byte)MapAutotileMode.Cliff == 4, "Autotile Cliff=4");
        Expect((byte)MapAutotileMode.Waterfall == 5, "Autotile Waterfall=5");
        Expect((byte)MapAutotileMode.Xp == 6, "Autotile XP=6");
        Expect((byte)MapAutotileMode.AnimatedXp == 7, "Autotile AnimatedXP=7");
    }

    private static void VerifyDefaultLayers()
    {
        var layers = MapLayerDefaults.Create();
        Expect(layers.Select(static value => value.Key).SequenceEqual(["Ground", "Mask 1", "Mask 2", "Fringe 1", "Fringe 2"]),
            "Capas Intersect por defecto");
        Expect(layers[0].Band == MapLayerBand.Lower && layers[1].Band == MapLayerBand.Lower && layers[2].Band == MapLayerBand.Lower,
            "Ground/Mask son Lower");
        Expect(layers[3].Band == MapLayerBand.Middle && layers[4].Band == MapLayerBand.Upper,
            "Fringe respeta Middle/Upper");
    }

    private static void VerifyNormalAutotilePatterns()
    {
        var key = new ContentKey("tileset.test");
        var center = new Vector2IntData(1, 1);

        var isolatedLayer = new MapLayerDefinition(
            "Ground",
            0,
            [new MapTilePlacementDefinition(center, key, default, autotile: MapAutotileMode.Normal)]);
        var isolated = new MapAutotileResolver(isolatedLayer, 3, 3, new(32, 32)).Resolve(center);
        Expect(isolated.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(0, 32), new Vector2IntData(48, 32), new Vector2IntData(0, 80), new Vector2IntData(48, 80)]),
            "Autotile aislado usa quarters interiores e/j/o/t");

        var filledTiles = new List<MapTilePlacementDefinition>();
        for (var y = 0; y < 3; y++)
        for (var x = 0; x < 3; x++)
            filledTiles.Add(new MapTilePlacementDefinition(new(x, y), key, default, autotile: MapAutotileMode.Normal));

        var filledLayer = new MapLayerDefinition("Ground", 0, filledTiles.ToArray());
        var filled = new MapAutotileResolver(filledLayer, 3, 3, new(32, 32)).Resolve(center);
        Expect(filled.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(32, 64), new Vector2IntData(16, 64), new Vector2IntData(32, 48), new Vector2IntData(16, 48)]),
            "Autotile rodeado usa fill q/n/k/h");
    }

    private static void VerifyFakeAndWaterfall()
    {
        var key = new ContentKey("tileset.test");
        var center = new Vector2IntData(1, 1);
        var tileSize = new Vector2IntData(32, 32);

        var fake = new MapAutotileResolver(
            Layer("Ground", new MapTilePlacementDefinition(center, key, default, autotile: MapAutotileMode.Fake)),
            3, 3, tileSize).Resolve(center);
        Expect(fake.RenderState == MapTileRenderState.Normal && fake.QuarterSourcePixels.Length == 0,
            "Fake no genera quarter-tiles");

        var normalBesideFake = new MapAutotileResolver(
            Layer("Ground",
                new MapTilePlacementDefinition(center, key, default, autotile: MapAutotileMode.Normal),
                new MapTilePlacementDefinition(new(2, 1), key, default, autotile: MapAutotileMode.Fake)),
            3, 3, tileSize).Resolve(center);
        var isolatedNormal = new MapAutotileResolver(
            Layer("Ground", new MapTilePlacementDefinition(center, key, default, autotile: MapAutotileMode.Normal)),
            3, 3, tileSize).Resolve(center);
        Expect(!normalBesideFake.QuarterSourcePixels.SequenceEqual(isolatedNormal.QuarterSourcePixels),
            "Fake conecta con el autotile vecino");

        var waterfallIsolated = ResolveMode(MapAutotileMode.Waterfall, center, filled: false);
        Expect(waterfallIsolated.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(0, 32), new Vector2IntData(48, 32), new Vector2IntData(0, 48), new Vector2IntData(48, 48)]),
            "Waterfall aislado usa e/j/g/l");

        var waterfallFilled = new MapAutotileResolver(
            Layer("Ground",
                new MapTilePlacementDefinition(new(0, 1), key, default, autotile: MapAutotileMode.Waterfall),
                new MapTilePlacementDefinition(center, key, default, autotile: MapAutotileMode.Waterfall),
                new MapTilePlacementDefinition(new(2, 1), key, default, autotile: MapAutotileMode.Waterfall)),
            3, 3, tileSize).Resolve(center);
        Expect(waterfallFilled.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(32, 32), new Vector2IntData(16, 32), new Vector2IntData(32, 48), new Vector2IntData(16, 48)]),
            "Waterfall con vecinos laterales usa i/f/k/h");
    }

    private static void VerifyCliffAndXp()
    {
        var key = new ContentKey("tileset.test");
        var center = new Vector2IntData(1, 1);
        var tileSize = new Vector2IntData(32, 32);

        var isolatedCliff = ResolveMode(MapAutotileMode.Cliff, center, filled: false);
        Expect(isolatedCliff.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(0, 32), new Vector2IntData(48, 32), new Vector2IntData(0, 80), new Vector2IntData(48, 80)]),
            "Cliff aislado usa quarters interiores e/j/o/t");

        var stacked = new MapAutotileResolver(
            Layer("Ground",
                new MapTilePlacementDefinition(new(1, 0), key, default, autotile: MapAutotileMode.Cliff),
                new MapTilePlacementDefinition(center, key, default, autotile: MapAutotileMode.Cliff)),
            3, 3, tileSize).Resolve(center);
        Expect(stacked.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(0, 64), new Vector2IntData(48, 64), new Vector2IntData(0, 80), new Vector2IntData(48, 80)]),
            "Cliff apilado usa vertical m/r y fondo o/t");

        var isolatedXp = ResolveMode(MapAutotileMode.Xp, center, filled: false);
        Expect(isolatedXp.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(0, 32), new Vector2IntData(80, 32), new Vector2IntData(0, 112), new Vector2IntData(80, 112)]),
            "XP aislado usa e/j/o/t del template XP");

        var filledXp = ResolveMode(MapAutotileMode.Xp, center, filled: true);
        Expect(filledXp.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(32, 64), new Vector2IntData(48, 64), new Vector2IntData(32, 80), new Vector2IntData(48, 80)]),
            "XP rodeado usa fill A/B/C/D");

        var filledAnimatedXp = ResolveMode(MapAutotileMode.AnimatedXp, center, filled: true);
        Expect(filledAnimatedXp.QuarterSourcePixels.SequenceEqual(filledXp.QuarterSourcePixels),
            "AnimatedXP resuelve la misma topología que XP");
    }

    private static void VerifyNeighborMapsAndAffectedCells()
    {
        var key = new ContentKey("tileset.test");
        var tileSize = new Vector2IntData(32, 32);
        var origin = new Vector2IntData(0, 0);
        var edgeLayer = Layer("Ground", new MapTilePlacementDefinition(origin, key, default, autotile: MapAutotileMode.Normal));

        var assumedOutside = new MapAutotileResolver(edgeLayer, 1, 1, tileSize).Resolve(origin);
        Expect(assumedOutside.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(32, 64), new Vector2IntData(16, 64), new Vector2IntData(32, 48), new Vector2IntData(16, 48)]),
            "Sin mapas vecinos el borde asume match");

        var emptyOutside = new MapAutotileResolver(edgeLayer, 1, 1, tileSize, static _ => null).Resolve(origin);
        Expect(emptyOutside.QuarterSourcePixels.SequenceEqual(
            [new Vector2IntData(0, 32), new Vector2IntData(48, 32), new Vector2IntData(0, 80), new Vector2IntData(48, 80)]),
            "Lookup nulo trata el borde como aislado");

        var neighborTile = new MapTilePlacementDefinition(new(-1, 0), key, default, autotile: MapAutotileMode.Normal);
        var withNeighbor = new MapAutotileResolver(edgeLayer, 1, 1, tileSize, cell => cell.X == -1 && cell.Y == 0 ? neighborTile : null)
            .Resolve(origin);
        Expect(!withNeighbor.QuarterSourcePixels.SequenceEqual(emptyOutside.QuarterSourcePixels),
            "Tile del mapa vecino participa en el match");

        var filledTiles = new List<MapTilePlacementDefinition>();
        for (var y = 0; y < 3; y++)
        for (var x = 0; x < 3; x++)
            filledTiles.Add(new MapTilePlacementDefinition(new(x, y), key, default, autotile: MapAutotileMode.Normal));
        var filledResolver = new MapAutotileResolver(Layer("Ground", filledTiles.ToArray()), 3, 3, tileSize);
        Expect(filledResolver.AffectedCells(new(1, 1)).Count == 9, "Normal afecta el 3×3");
        Expect(!filledResolver.RequiresFullRefresh, "Normal no exige refresh completo");

        var cliffLayer = Layer("Ground",
            new MapTilePlacementDefinition(new(0, 0), key, default, autotile: MapAutotileMode.Cliff),
            new MapTilePlacementDefinition(new(2, 2), key, default, autotile: MapAutotileMode.Cliff));
        var cliffResolver = new MapAutotileResolver(cliffLayer, 3, 3, tileSize);
        var affectedCliffs = cliffResolver.AffectedCells(new(0, 0));
        Expect(affectedCliffs.Contains(new Vector2IntData(0, 0)) && affectedCliffs.Contains(new Vector2IntData(2, 2)),
            "Cliff incluye todos los cliffs de la capa");
        Expect(cliffResolver.RequiresFullRefresh, "Cliff exige refresh completo del viewport");
    }

    private static void VerifyAlternativeAtlasOffset()
    {
        var key = new ContentKey("tileset.test");
        var center = new Vector2IntData(1, 1);
        var baseTile = new MapTilePlacementDefinition(center, key, default, autotile: MapAutotileMode.Normal);
        var altTile = new MapTilePlacementDefinition(center, key, default, alternative: 1, autotile: MapAutotileMode.Normal);
        var tileSize = new Vector2IntData(32, 32);

        Expect(altTile.AtlasPixelOrigin(tileSize) == new Vector2IntData(0, 32), "Alternative desplaza una fila de atlas");

        var baseQuarters = new MapAutotileResolver(Layer("Ground", baseTile), 3, 3, tileSize).Resolve(center).QuarterSourcePixels;
        var altQuarters = new MapAutotileResolver(Layer("Ground", altTile), 3, 3, tileSize).Resolve(center).QuarterSourcePixels;
        Expect(altQuarters.SequenceEqual(baseQuarters.Select(static pixel => pixel + new Vector2IntData(0, 32))),
            "Alternative desplaza los quarter-tiles");

        var mixed = new MapAutotileResolver(
            Layer("Ground", baseTile, new MapTilePlacementDefinition(new(2, 1), key, default, alternative: 1, autotile: MapAutotileMode.Normal)),
            3, 3, tileSize).Resolve(center);
        var isolated = new MapAutotileResolver(Layer("Ground", baseTile), 3, 3, tileSize).Resolve(center);
        Expect(mixed.QuarterSourcePixels.SequenceEqual(isolated.QuarterSourcePixels),
            "Alternative distinto no conecta autotiles");
    }

    private static void VerifyAnimationOffsets()
    {
        var tile = new Vector2IntData(32, 32);
        Expect(MapAutotileRenderData.FrameOffset(MapAutotileMode.Animated, 1, 0, tile) == new Vector2IntData(64, 0),
            "Animated avanza 2 tiles por frame");
        Expect(MapAutotileRenderData.FrameOffset(MapAutotileMode.AnimatedXp, 2, 0, tile) == new Vector2IntData(192, 0),
            "AnimatedXP avanza 3 tiles por frame");
        Expect(MapAutotileRenderData.FrameOffset(MapAutotileMode.Waterfall, 0, 0, tile) == new Vector2IntData(0, -32),
            "Waterfall frame 0 usa fila superior");
        Expect(MapAutotileRenderData.FrameOffset(MapAutotileMode.Waterfall, 0, 2, tile) == new Vector2IntData(0, 32),
            "Waterfall frame 2 usa fila inferior");
        Expect(MapAutotileRenderData.FrameOffset(MapAutotileMode.Cliff, 0, 0, tile) == new Vector2IntData(0, -32),
            "Cliff usa fila superior compatible con Intersect");
    }

    private static void VerifyTilesetPackageRoundtrip()
    {
        Expect(TilesetDefinition.IsAtlasSizeCompatible(64, 32, new(32, 32)), "Atlas múltiplo válido");
        Expect(!TilesetDefinition.IsAtlasSizeCompatible(65, 32, new(32, 32)), "Atlas con ancho no múltiplo");
        Expect(!TilesetDefinition.IsAtlasSizeCompatible(64, 31, new(32, 32)), "Atlas con alto no múltiplo");
        Expect(!TilesetDefinition.IsAtlasSizeCompatible(0, 32, new(32, 32)), "Atlas vacío rechazado");

        var tileset = new TilesetDefinition(
            DefinitionId.New(),
            new ContentKey("tileset.test"),
            "Test Tileset",
            string.Empty,
            true,
            1,
            null,
            new ContentKey("test"),
            new Vector2IntData(32, 32));

        var layer = new MapLayerDefinition(
            "Ground",
            0,
            [new MapTilePlacementDefinition(default, tileset.Key, default, autotile: MapAutotileMode.Animated)],
            band: MapLayerBand.Lower);
        var map = new MapDefinition(
            DefinitionId.New(),
            new ContentKey("maps.autotile_test"),
            "Autotile Test",
            string.Empty,
            true,
            1,
            null,
            new ContentKey("maps.autotile_test.visual"),
            new BoundsData(new(0, 0), new(96, 96)),
            new Vector2Data(16, 16),
            new Vector2IntData(32, 32),
            new MapContentDefinition(layers: [layer]));

        var package = ContentPackage.Empty("autotile-test") with
        {
            Maps = [map],
            Tilesets = [tileset]
        };
        Expect(package.Validate().Count == 0, "Package con tileset válido");

        var roundtrip = ContentPackage.FromJson(package.ToJson());
        Expect(roundtrip.Tilesets.Length == 1, "Tileset sobrevive JSON roundtrip");
        Expect(roundtrip.Maps[0].Content.Layers[0].Tiles[0].Autotile == MapAutotileMode.Animated,
            "AutotileMode sobrevive JSON roundtrip");
        Expect(roundtrip.Maps[0].Content.Layers[0].Band == MapLayerBand.Lower,
            "LayerBand sobrevive JSON roundtrip");

        var missingTileset = ContentPackage.Empty("autotile-test") with { Maps = [map] };
        Expect(missingTileset.Validate().Any(static error => error.Contains("TilesetDefinition inexistente", StringComparison.Ordinal)),
            "Package rechaza tileset faltante");
    }

    private static MapAutotileRenderData ResolveMode(MapAutotileMode mode, Vector2IntData center, bool filled)
    {
        var key = new ContentKey("tileset.test");
        var tiles = new List<MapTilePlacementDefinition>();
        if (filled)
        {
            for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
                tiles.Add(new MapTilePlacementDefinition(new(x, y), key, default, autotile: mode));
        }
        else
        {
            tiles.Add(new MapTilePlacementDefinition(center, key, default, autotile: mode));
        }

        return new MapAutotileResolver(Layer("Ground", tiles.ToArray()), 3, 3, new(32, 32)).Resolve(center);
    }

    private static MapLayerDefinition Layer(string key, params MapTilePlacementDefinition[] tiles)
        => new(key, 0, tiles);

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("MapAutotileVerification FAIL: " + name);
    }
}
