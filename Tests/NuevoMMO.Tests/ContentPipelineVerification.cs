using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using NuevoMMO.Core;
using NuevoMMO.Editor;
using NuevoMMO.Server;
using NuevoMMO.Server.Configuration;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Entities;

internal static class ContentPipelineVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"nuevommo-pipeline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var gamePath = Path.Combine(directory, "game.db");
        try
        {
            var mob = new MobDefinition(
                DefinitionId.New(), new ContentKey("mobs.pipeline"), "Lobo de contenido", string.Empty, true, 1,
                ["content"], new ContentKey("template.player"),
                combat: new CreatureCombatDefinition(level: 2, experience: 15, baseDamage: 4));
            var npc = new NpcDefinition(
                DefinitionId.New(), new ContentKey("npcs.pipeline"), "Aldeano", string.Empty, true, 1, null,
                new ContentKey("template.player"));
            var resource = new ResourceDefinition(
                DefinitionId.New(), new ContentKey("resources.pipeline"), "Mena", string.Empty, true, 1, null,
                new ContentKey("template.player"),
                harvest: new ResourceHarvestDefinition(healthRange: NumericRange.Fixed(8)));
            var table = new SpawnTableDefinition(
                DefinitionId.New(), new ContentKey("spawns.pipeline"), "Manada", string.Empty, true, 1, null,
                [mob.Id],
                entries: [new SpawnEntryDefinition(SpawnEntityKind.Mob, mob.Id, maximumAlive: 2)],
                maximumTotalAlive: 2);
            var map = new MapDefinition(
                DefinitionId.New(), new ContentKey("maps.pipeline"), "Campo", string.Empty, true, 1, ["content"],
                new ContentKey("maps.pipeline.visual"),
                new BoundsData(new(0, 0), new(960, 640)),
                new Vector2Data(240, 240),
                new Vector2IntData(32, 32),
                new MapContentDefinition(
                    placements:
                    [
                        new MapContentPlacementDefinition(Guid.NewGuid(), SpawnEntityKind.Mob, mob.Id, new Vector2Data(128, 128)),
                        new MapContentPlacementDefinition(Guid.NewGuid(), SpawnEntityKind.Npc, npc.Id, new Vector2Data(160, 160)),
                        new MapContentPlacementDefinition(Guid.NewGuid(), SpawnEntityKind.Resource, resource.Id, new Vector2Data(192, 192))
                    ],
                    spawnZones:
                    [
                        new MapSpawnZoneDefinition(
                            Guid.NewGuid(),
                            table.Id,
                            new MapShapeDefinition(MapShapeKind.Rectangle, new Vector2Data(400, 240), new Vector2Data(80, 40)),
                            maximumAliveOverride: 2)
                    ]));

            var package = ContentPackage.Empty("pipeline-1") with
            {
                Maps = [map],
                Mobs = [mob],
                Npcs = [npc],
                Resources = [resource],
                SpawnTables = [table]
            };
            GameDatabase.Save(gamePath, package);
            Expect(GameDataSqlite.TryLoad(gamePath, out var loaded) && loaded is not null && loaded.Maps[0].Id == map.Id,
                "Servidor lee el mismo game.db que escribió el Editor");

            var configuration = ServerConfiguration.Development() with
            {
                Database = new DatabaseConfiguration
                {
                    Provider = "sqlite",
                    AutoMigrate = true,
                    Sqlite = new SqliteDatabaseFiles
                    {
                        Auth = Path.Combine(directory, "auth.db"),
                        Players = Path.Combine(directory, "players.db"),
                        Game = gamePath,
                        Logs = Path.Combine(directory, "logs.db")
                    }
                }
            };

            var composition = DevelopmentWorldFactory.CreateAsync("Development", configuration)
                .GetAwaiter().GetResult();
            Expect(composition.World.Map.Id == map.Id, "El mundo arranca con el mapa de game.db");
            Expect(composition.World.MapInstance.Entities.All.OfType<Mob>().Any(value => value.DefinitionId == mob.Id),
                "Placement de mob aparece en el runtime");
            Expect(composition.World.MapInstance.Entities.All.OfType<Npc>().Any(value => value.DefinitionId == npc.Id),
                "Placement de NPC aparece en el runtime");
            Expect(composition.World.MapInstance.Entities.All.OfType<ResourceEntity>().Any(value => value.DefinitionId == resource.Id),
                "Placement de recurso aparece en el runtime");
            Expect(composition.World.MapInstance.Entities.All.OfType<Mob>().Count(value => value.DefinitionId == mob.Id) >= 3,
                "Spawn zone instancia la tabla además del placement");
            Expect(composition.World.MapInstance.Entities.All.OfType<Mob>().Any(value => value.DefinitionId == TrainingDummyFixture.DefinitionId),
                "El dummy de desarrollo sigue disponible para combate");
            Expect(composition.PersistenceDescription.Contains("game.db", StringComparison.Ordinal),
                "La descripción de persistencia indica game.db");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("ContentPipelineVerification FAIL: " + name);
    }
}
