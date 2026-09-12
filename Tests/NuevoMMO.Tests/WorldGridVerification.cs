using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Editor;

internal static class WorldGridVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyCreateNeighbors();
        VerifyCannotOverlap();
        VerifyAssignUniqueCells();
        VerifyGameDataLocator();
    }

    private static void VerifyCreateNeighbors()
    {
        var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
        var origin = editor.Maps.CreateAt(0, 0);
        Expect(origin.GridX == 0 && origin.GridY == 0, "Primer mapa en (0,0)");
        Expect(MapWorldGrid.CanCreate(editor.Definitions, 1, 0), "Celda este vacía es creable");
        Expect(!MapWorldGrid.CanCreate(editor.Definitions, 2, 0), "Celda no adyacente no es creable");

        var east = editor.Maps.CreateAt(1, 0);
        Expect(east.WestMapId == origin.Id, "Este enlaza al origen por el oeste");
        var originReloaded = editor.Definitions.Get<MapDefinition>(origin.Id);
        Expect(originReloaded.EastMapId == east.Id, "Origen enlaza al este");
        Expect(east.GridX == 1 && east.GridY == 0, "Mapa este en (1,0)");
    }

    private static void VerifyCannotOverlap()
    {
        var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
        editor.Maps.CreateAt(0, 0);
        try
        {
            editor.Maps.CreateAt(0, 0);
            throw new Exception("FAIL: No se rechazó mapa duplicado en (0,0)");
        }
        catch (InvalidOperationException)
        {
            Expect(true, "No se puede crear dos mapas en la misma celda");
        }
    }

    private static void VerifyAssignUniqueCells()
    {
        var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
        editor.Definitions.Register(NewMap("maps.a"));
        editor.Definitions.Register(NewMap("maps.b"));
        var moved = MapWorldGrid.AssignUniqueCells(editor.Definitions);
        Expect(moved == 1, "El segundo mapa en (0,0) se desplaza");
        var cells = editor.Definitions.GetAll<MapDefinition>().Select(map => (map.GridX, map.GridY)).ToHashSet();
        Expect(cells.Count == 2, "Celdas de mapa únicas tras asignar");
    }

    private static void VerifyGameDataLocator()
    {
        var path = GameDatabase.LocateSharedPath();
        Expect(path.Replace('\\', '/').EndsWith("Data/game.db", StringComparison.OrdinalIgnoreCase),
            "LocateSharedPath apunta a Data/game.db");
        Expect(Directory.Exists(Path.GetDirectoryName(path)!), "Carpeta Data existe o se crea");
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

    private static void Expect(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("WorldGridVerification FAIL: " + name);
    }
}
