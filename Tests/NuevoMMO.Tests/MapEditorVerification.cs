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

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("MapEditorVerification FAIL: " + name);
    }
}
