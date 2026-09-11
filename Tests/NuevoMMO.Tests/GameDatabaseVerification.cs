using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using NuevoMMO.Core;
using NuevoMMO.Editor;

internal static class GameDatabaseVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nuevommo-gamedata-{Guid.NewGuid():N}.db");
        try
        {
            var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
            var item = new ItemDefinition(
                DefinitionId.New(), new ContentKey("items.potion"), "Poción", "Cura.", true, 1, ["item"],
                new ContentKey("visuals.item.potion"), kind: ItemKind.Consumable);
            var mob = new MobDefinition(
                DefinitionId.New(), new ContentKey("mobs.wolf"), "Lobo", string.Empty, true, 1, ["mob"],
                new ContentKey("visuals.mob.wolf"),
                behavior: new CreatureBehaviorDefinition(aggressive: true, sightRange: 8),
                combat: new CreatureCombatDefinition(level: 3, experience: 40, baseDamage: 12));
            editor.Definitions.Register(item);
            editor.Definitions.Register(mob);
            editor.Dirty.Mark();
            editor.Content.Save(path);

            Expect(File.Exists(path), "game.db se escribe en disco");
            Expect(GameDatabase.IsDatabasePath(path), "La extensión .db identifica GameData");

            var reopened = new EditorApplication(new() { Mode = EditorMode.Offline });
            reopened.Content.Load(path);
            Expect(reopened.Definitions.Get<ItemDefinition>(item.Id).Name == "Poción", "Item sobrevive game.db");
            Expect(reopened.Definitions.Get<MobDefinition>(mob.Id).Combat.Level == 3, "Mob.Combat sobrevive game.db");
            Expect(reopened.Definitions.Get<MobDefinition>(mob.Id).Behavior.Aggressive, "Mob.Behavior sobrevive game.db");
            Expect(!reopened.Dirty.IsDirty, "Load de game.db deja el proyecto limpio");
            Expect(NuevoMMO.Server.Database.GameDataSqlite.TryLoad(path, out var serverPackage)
                && serverPackage is not null
                && serverPackage.Mobs.Any(value => value.Id == mob.Id),
                "El servidor carga el game.db del Editor");

            try
            {
                reopened.Content.Save(Path.ChangeExtension(path, ".json"));
                throw new InvalidOperationException("GameDatabaseVerification FAIL: JSON no debe guardarse");
            }
            catch (InvalidDataException)
            {
                // esperado
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
                // El archivo temporal puede quedar bloqueado un instante en Windows.
            }
        }
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("GameDatabaseVerification FAIL: " + name);
    }
}
