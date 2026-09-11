using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Editor;

internal static class MapEditorVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifySaveDoesNotClearProjectDirty();
        VerifyDefaultsOnlyOnCreate();
        VerifyExplicitLayerMigrationMarksDirty();
        VerifyTypedContinuousPlacementsAndHistory();
        VerifySpawnZoneHistory();
        VerifyFillDragCopyPasteAndPlacements();
    }

    private static void VerifySaveDoesNotClearProjectDirty()
    {
        var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
        var item = new ItemDefinition(
            DefinitionId.New(), new ContentKey("items.dirty"), "Item", string.Empty, true, 1, null,
            new ContentKey("visuals.item"));
        editor.Definitions.Register(item);
        editor.Dirty.Mark();

        var map = NewMap("maps.dirty_save");
        editor.Maps.Create(map);
        editor.Maps.Save();

        Expect(editor.Dirty.IsDirty, "Guardar mapa no limpia el Dirty global del proyecto");
        editor.Content.Save(Path.Combine(Path.GetTempPath(), $"nuevommo-dirty-{Guid.NewGuid():N}.db"));
        Expect(!editor.Dirty.IsDirty, "ContentWorkspace.Save sí limpia el Dirty global");
    }

    private static void VerifyDefaultsOnlyOnCreate()
    {
        var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
        var created = editor.Maps.Create(NewMap("maps.created"));
        Expect(created.Layers.Count == 5, "Crear mapa inserta capas Intersect");
        Expect(created.Layers.Select(static layer => layer.Key).SequenceEqual(["Ground", "Mask 1", "Mask 2", "Fringe 1", "Fringe 2"]),
            "Capas por defecto al crear");

        var empty = NewMap("maps.opened_empty");
        editor.Definitions.Register(empty);
        editor.Dirty.Clear();
        var opened = editor.Maps.Open(empty);
        Expect(opened.Layers.Count == 0, "Abrir un mapa vacío no inserta capas");
        Expect(!editor.Dirty.IsDirty, "Abrir no marca Dirty");
    }

    private static void VerifyExplicitLayerMigrationMarksDirty()
    {
        var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
        var empty = NewMap("maps.migrate");
        editor.Definitions.Register(empty);
        editor.Maps.Open(empty);
        editor.Dirty.Clear();

        Expect(editor.Maps.EnsureDefaultLayers(), "Migración explícita de capas vacías");
        Expect(editor.Maps.Document!.Layers.Count == 5, "Migración inserta defaults");
        Expect(editor.Dirty.IsDirty, "Migración explícita marca Dirty");
        Expect(!editor.Maps.EnsureDefaultLayers(), "No remigra un mapa que ya tiene capas");
    }

    private static void VerifyTypedContinuousPlacementsAndHistory()
    {
        var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
        var mob = NewMob("mobs.map_test");
        var npc = NewNpc("npcs.map_test");
        var resource = NewResource("resources.map_test");
        editor.Definitions.Register(mob);
        editor.Definitions.Register(npc);
        editor.Definitions.Register(resource);
        editor.Maps.Create(NewMap("maps.placements"));
        editor.Dirty.Clear();

        Expect(editor.Maps.PlacementDefinitions(SpawnEntityKind.Mob).Single().Id == mob.Id,
            "Selector Mob solo devuelve MobDefinition");
        Expect(editor.Maps.PlacementDefinitions(SpawnEntityKind.Npc).Single().Id == npc.Id,
            "Selector NPC solo devuelve NpcDefinition");
        Expect(editor.Maps.PlacementDefinitions(SpawnEntityKind.Resource).Single().Id == resource.Id,
            "Selector Resource solo devuelve ResourceDefinition");

        var originalPosition = new Vector2Data(123.5f, 77.25f);
        var placement = editor.Maps.Place(SpawnEntityKind.Mob, mob.Id, originalPosition);
        Expect(placement.Position == originalPosition, "Placement conserva coordenadas continuas sin snap a tile");
        Expect(editor.Maps.PreviewDefinition().Content.Placements.Single().Id == placement.Id,
            "Placement queda integrado en MapDefinition");
        Expect(editor.Dirty.IsDirty, "Placement marca Dirty");

        ExpectThrows<InvalidOperationException>(
            () => editor.Maps.Place(SpawnEntityKind.Mob, npc.Id, new(160.75f, 80.5f)),
            "No permite usar NpcDefinition como Mob placement");
        ExpectThrows<ArgumentOutOfRangeException>(
            () => editor.Maps.MovePlacement(placement.Id, new(-1, 20)),
            "No permite mover placement fuera del mapa");

        var movedPosition = new Vector2Data(191.75f, 144.125f);
        var moved = editor.Maps.MovePlacement(placement.Id, movedPosition);
        Expect(moved.Id == placement.Id && moved.Position == movedPosition,
            "Mover conserva identidad y coordenada continua");

        editor.History.Undo();
        Expect(editor.Maps.FindPlacement(placement.Id)?.Position == originalPosition,
            "Undo restaura posición anterior");
        editor.History.Redo();
        Expect(editor.Maps.FindPlacement(placement.Id)?.Position == movedPosition,
            "Redo reaplica movimiento");

        Expect(editor.Maps.RemovePlacement(placement.Id), "Borrar placement existente");
        Expect(editor.Maps.FindPlacement(placement.Id) is null, "Placement desaparece al borrar");
        editor.History.Undo();
        Expect(editor.Maps.FindPlacement(placement.Id)?.Position == movedPosition,
            "Undo de borrado restaura placement completo");
    }

    private static void VerifySpawnZoneHistory()
    {
        var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
        var mob = NewMob("mobs.spawn_zone");
        editor.Definitions.Register(mob);
        var spawnTable = new SpawnTableDefinition(
            DefinitionId.New(),
            new ContentKey("spawns.map_test"),
            "Spawn Map Test",
            string.Empty,
            true,
            1,
            null,
            [mob.Id]);
        editor.Definitions.Register(spawnTable);
        editor.Maps.Create(NewMap("maps.spawn_zone"));
        editor.Dirty.Clear();

        Expect(editor.Maps.SpawnTableDefinitions().Single().Id == spawnTable.Id,
            "Selector SpawnZone usa SpawnTableDefinition");
        var area = new MapShapeDefinition(MapShapeKind.Rectangle, new(220.5f, 180.25f), new(125.5f, 83.75f));
        var zone = editor.Maps.AddSpawnZone(spawnTable.Id, area);
        Expect(editor.Maps.FindSpawnZone(zone.Id)?.Area == area,
            "SpawnZone conserva geometría continua");

        editor.History.Undo();
        Expect(editor.Maps.FindSpawnZone(zone.Id) is null, "Undo elimina SpawnZone recién creada");
        editor.History.Redo();
        Expect(editor.Maps.FindSpawnZone(zone.Id)?.SpawnTableId == spawnTable.Id,
            "Redo restaura SpawnZone y su tabla");
        Expect(editor.Maps.RemoveSpawnZone(zone.Id), "Borrar SpawnZone existente");
        editor.History.Undo();
        Expect(editor.Maps.FindSpawnZone(zone.Id) is not null, "Undo de borrado restaura SpawnZone");
    }

    private static void VerifyFillDragCopyPasteAndPlacements()
    {
        var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
        editor.Maps.Create(NewMap("maps.tools"));
        editor.Maps.Palette.Select(new ContentKey("tilesets.ground"), new Vector2IntData(1, 2));

        Expect(editor.Maps.PaintRect(new(0, 0), new(1, 0)) == 2, "Pintar rectángulo cubre dos celdas");
        Expect(editor.Maps.Document!.Layers[0].Tiles.Length == 2, "El rectángulo queda en la capa activa");

        editor.Maps.Palette.Select(new ContentKey("tilesets.ground"), new Vector2IntData(3, 4));
        Expect(editor.Maps.Fill(new(0, 1)) > 2, "Fill pinta las celdas vacías conectadas");

        var clipboard = editor.Maps.Copy([new(0, 0), new(1, 0)]);
        Expect(editor.Maps.Paste(new(4, 0), clipboard) == 2, "Copy/paste mueve el bloque relativo");
        Expect(editor.Maps.Document.Layers[0].Tiles.Any(tile => tile.Cell == new Vector2IntData(4, 0)), "Paste escribe en el origen");

        var mob = new MobDefinition(
            DefinitionId.New(), new ContentKey("mobs.editor"), "Lobo", string.Empty, true, 1, null,
            new ContentKey("visuals.mob"));
        editor.Definitions.Register(mob);
        editor.Maps.Place(SpawnEntityKind.Mob, mob.Id, new Vector2Data(64, 64));
        Expect(editor.Maps.Document.Placements.Count == 1, "Placement de mob queda en el mapa");

        var table = new SpawnTableDefinition(
            DefinitionId.New(), new ContentKey("spawns.wolves"), "Lobos", string.Empty, true, 1, null,
            [mob.Id]);
        editor.Definitions.Register(table);
        var zone = editor.Maps.PlaceSpawnZone(
            table.Id,
            new MapShapeDefinition(MapShapeKind.Rectangle, new Vector2Data(128, 128), new Vector2Data(64, 64)));
        Expect(editor.Maps.Document.SpawnZones.Count == 1 && zone.SpawnTableId == table.Id, "Zona de spawn usa la tabla");

        var portal = editor.Maps.PlacePortal(
            new MapShapeDefinition(MapShapeKind.Rectangle, new Vector2Data(200, 200), new Vector2Data(32, 32)),
            editor.Maps.Document.Id,
            editor.Maps.Document.Spawn);
        Expect(editor.Maps.Document.Portals.Count == 1 && portal.DestinationMapId == editor.Maps.Document.Id,
            "Portal vecino apunta a un mapa existente");

        editor.Maps.PlaceRegion("region.north", "Norte", new MapShapeDefinition(
            MapShapeKind.Rectangle, new Vector2Data(300, 300), new Vector2Data(48, 48)));
        editor.Maps.PlaceLight(new Vector2Data(80, 80));
        editor.Maps.PlaceEvent(new Vector2Data(96, 96));
        Expect(editor.Maps.Document.Regions.Count == 1, "Región colocada");
        Expect(editor.Maps.Document.Lights.Count == 1, "Luz colocada");
        Expect(editor.Definitions.GetAll<EventDefinition>().Count == 1, "Evento de mapa registrado");
    }

    private static MapDefinition NewMap(string key)
        => new(
            DefinitionId.New(),
            new ContentKey(key),
            key,
            string.Empty,
            true,
            1,
            null,
            new ContentKey(key + ".visual"),
            new BoundsData(new(0, 0), new(960, 640)),
            new Vector2Data(64, 64),
            new Vector2IntData(32, 32));

    private static MobDefinition NewMob(string key)
        => new(
            DefinitionId.New(),
            new ContentKey(key),
            key,
            string.Empty,
            true,
            1,
            null,
            new ContentKey(key + ".visual"));

    private static NpcDefinition NewNpc(string key)
        => new(
            DefinitionId.New(),
            new ContentKey(key),
            key,
            string.Empty,
            true,
            1,
            null,
            new ContentKey(key + ".visual"));

    private static ResourceDefinition NewResource(string key)
        => new(
            DefinitionId.New(),
            new ContentKey(key),
            key,
            string.Empty,
            true,
            1,
            null,
            new ContentKey(key + ".visual"));

    private static void ExpectThrows<T>(Action action, string name) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException("MapEditorVerification FAIL: " + name);
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("MapEditorVerification FAIL: " + name);
    }
}
