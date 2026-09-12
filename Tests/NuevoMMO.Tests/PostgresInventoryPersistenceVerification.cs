using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Entities;

internal static class PostgresInventoryPersistenceVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        var connectionString = Environment.GetEnvironmentVariable("NUEVOMMO_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        VerifyAsync(connectionString).GetAwaiter().GetResult();
    }

    private static async Task VerifyAsync(string connectionString)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await PostgresMigrator.ApplyAsync(connectionString, timeout.Token);

        var accounts = new PostgresAccountRepository(connectionString);
        var characters = new PostgresCharacterRepository(connectionString);
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var account = await accounts.CreateAsync($"ci_{suffix}", "integration-test-hash", timeout.Token);
        var mapId = DefinitionId.New();
        var created = await characters.CreateAsync(
            account.Id,
            $"Hero_{suffix}",
            mapId,
            new Vector2Data(123.5f, 456.25f),
            DefinitionId.Empty,
            CanonicalCharacterAppearance.Default,
            timeout.Token);

        var itemId = Guid.NewGuid();
        var definitionId = Guid.NewGuid();
        var storage = new CharacterInventoryStorage
        {
            Items = [new StoredInventoryItem(itemId, definitionId, 3, 87)],
            Equipment = [new StoredEquipmentEntry((byte)EquipmentSlot.Weapon, 0, itemId)]
        };
        var json = storage.ToJson();
        await characters.SaveInventoryAsync(created.Id, json, timeout.Token);

        var questId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questStorage = new CharacterQuestStorage
        {
            Quests =
            [
                new StoredQuestProgress(
                    questId,
                    (byte)QuestRuntimeState.Active,
                    0,
                    [new StoredQuestTaskProgress(taskId, 2)])
            ]
        };
        var questJson = questStorage.ToJson();
        await characters.SaveQuestDataAsync(created.Id, questJson, timeout.Token);

        var reloaded = await characters.GetAsync(created.Id, timeout.Token)
            ?? throw new InvalidOperationException("PostgreSQL persistence FAIL: personaje no recargado.");
        Expect(reloaded.InventoryData == json, "inventory_data hace round-trip exacto");
        var parsed = CharacterInventoryStorage.Parse(reloaded.InventoryData);
        Expect(parsed.Items.Length == 1, "item persistido");
        Expect(parsed.Items[0].Id == itemId && parsed.Items[0].Quantity == 3 && parsed.Items[0].Durability == 87,
            "estado de item persistido");
        Expect(parsed.Equipment.Length == 1 && parsed.Equipment[0].ItemId == itemId,
            "referencia de equipo persistida");

        Expect(reloaded.QuestData == questJson, "quest_data hace round-trip exacto");
        var parsedQuests = CharacterQuestStorage.Parse(reloaded.QuestData);
        Expect(parsedQuests.Quests.Length == 1 && parsedQuests.Quests[0].QuestId == questId,
            "quest persistida");
        Expect(parsedQuests.Quests[0].State == (byte)QuestRuntimeState.Active &&
               parsedQuests.Quests[0].Tasks.Length == 1 &&
               parsedQuests.Quests[0].Tasks[0].TaskId == taskId &&
               parsedQuests.Quests[0].Tasks[0].Progress == 2,
            "estado y progreso de quest persistidos");
        Expect(reloaded.MapDefinition == mapId && reloaded.Position == new Vector2Data(123.5f, 456.25f),
            "identidad/mapa no se corrompen al guardar estado persistente");

        Console.WriteLine("PASS: PostgreSQL inventory/equipment/quest persistence round-trip");
    }

    private static void Expect(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("PostgresInventoryPersistenceVerification FAIL: " + name);
    }
}
