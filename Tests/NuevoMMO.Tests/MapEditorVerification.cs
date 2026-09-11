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
