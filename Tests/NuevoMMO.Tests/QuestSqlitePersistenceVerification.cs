using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using NuevoMMO.Core;
using NuevoMMO.Server.Configuration;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Entities;

internal static class QuestSqlitePersistenceVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyAsync().GetAwaiter().GetResult();
    }

    private static async Task VerifyAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), $"nuevommo-quest-sqlite-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var paths = new SqliteDatabasePaths(
            Path.Combine(root, "auth.db"),
            Path.Combine(root, "players.db"),
            Path.Combine(root, "game.db"),
            Path.Combine(root, "logs.db"));
        try
        {
            await SqliteMigrator.ApplyAsync(paths);
            var repository = new SqliteCharacterRepository(paths.Players);
            var account = new AccountId(Guid.NewGuid());
            var record = await repository.CreateAsync(
                account, "QuestSqlite", DefinitionId.New(), new Vector2Data(10, 20), DefinitionId.Empty);

            var questId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var storage = new CharacterQuestStorage
            {
                Quests =
                [
                    new StoredQuestProgress(
                        questId,
                        (byte)QuestRuntimeState.Active,
                        1,
                        [new StoredQuestTaskProgress(taskId, 4)])
                ]
            };
            var json = storage.ToJson();
            await repository.SaveQuestDataAsync(record.Id, json);

            var reloaded = await repository.GetAsync(record.Id)
                ?? throw new InvalidOperationException("Quest SQLite persistence FAIL: personaje no recargado.");
            Expect(reloaded.QuestData == json, "SQLite quest_data hace round-trip exacto");
            var parsed = CharacterQuestStorage.Parse(reloaded.QuestData);
            Expect(parsed.Quests.Length == 1 && parsed.Quests[0].QuestId == questId,
                "SQLite restaura quest persistida");
            Expect(parsed.Quests[0].State == (byte)QuestRuntimeState.Active &&
                   parsed.Quests[0].CompletionCount == 1 &&
                   parsed.Quests[0].Tasks.Length == 1 &&
                   parsed.Quests[0].Tasks[0].TaskId == taskId &&
                   parsed.Quests[0].Tasks[0].Progress == 4,
                "SQLite conserva estado, historial y progreso de tarea");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(root, recursive: true); }
            catch (IOException) { }
        }
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("QuestSqlitePersistenceVerification FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
