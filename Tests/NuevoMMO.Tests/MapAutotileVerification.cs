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

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("MapAutotileVerification FAIL: " + name);
    }
}
