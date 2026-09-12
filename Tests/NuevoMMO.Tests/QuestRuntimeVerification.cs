using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;

internal static class QuestRuntimeVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyEventOwnedQuestLifecycle();
        VerifyInvalidCompletionAndFailure();
        VerifyQuestPersistenceReconnect();
    }

    private static void VerifyEventOwnedQuestLifecycle()
    {
        var definitions = new DefinitionRegistry();
        var firstTask = new QuestTaskDefinition(Guid.NewGuid(), "Primera tarea", quantity: 2);
        var secondTask = new QuestTaskDefinition(Guid.NewGuid(), "Segunda tarea");
        var quest = new QuestDefinition(
            DefinitionId.New(),
            new ContentKey("quests.runtime.lifecycle"),
            "Quest Runtime",
            string.Empty,
            true,
            1,
            null,
            repeatable: true,
            tasks: [firstTask, secondTask]);
        definitions.Register(quest);

        var systems = new GameSystems(definitions);
        var player = CreatePlayer();
        var listId = Guid.NewGuid();
        var evt = new EventDefinition(
            DefinitionId.New(),
            new ContentKey("events.runtime.quest_lifecycle"),
            "Quest lifecycle",
            string.Empty,
            true,
            1,
            null,
            pages:
            [
                new EventPageDefinition(
                    Guid.NewGuid(),
                    commandLists: new Dictionary<Guid, EventCommandDefinition[]>
                    {
                        [listId] =
                        [
                            QuestCommand(EventCommandKind.StartQuest, quest.Id),
                            QuestCommand(EventCommandKind.AdvanceQuest, quest.Id,
                                numbers: new Dictionary<string, float> { ["amount"] = 2 },
                                text: new Dictionary<string, string> { ["task"] = firstTask.Id.ToString("D") }),
                            QuestCommand(EventCommandKind.AdvanceQuest, quest.Id,
                                text: new Dictionary<string, string> { ["task"] = secondTask.Id.ToString("D") }),
                            QuestCommand(EventCommandKind.CompleteQuest, quest.Id)
                        ]
                    },
                    rootCommandListId: listId)
            ]);

        var result = systems.Events.TryTrigger(player, evt, EventTrigger.Action, 1_000);
        Expect(result.Success, "evento ejecuta lifecycle de quest");
        Expect(player.Quests.TryGet(quest.Id, out var progress) && progress is not null,
            "quest queda registrada en journal del jugador");
        Expect(progress!.State == QuestRuntimeState.Completed && progress.CompletionCount == 1,
            "quest completa una vuelta");
        Expect(progress.ProgressOf(firstTask.Id) == 2 && progress.ProgressOf(secondTask.Id) == 1,
            "progreso de tareas pertenece al QuestSystem");
        Expect(result.Log.Any(line => line.StartsWith("quest_started:", StringComparison.Ordinal)) &&
               result.Log.Any(line => line.StartsWith("quest_completed:", StringComparison.Ordinal)),
            "EventRuntime reporta transiciones propietarias");

        var completedCondition = new ConditionDefinition(
            ConditionKind.QuestState,
            references: new Dictionary<string, DefinitionId> { ["quest"] = quest.Id },
            text: new Dictionary<string, string> { ["state"] = "Completed" });
        Expect(systems.Conditions.Evaluate(player, completedCondition),
            "ConditionSystem observa estado de quest sin mutarlo");

        systems.Quests.Start(player, quest.Id);
        Expect(progress.State == QuestRuntimeState.Active && progress.CompletionCount == 1 &&
               progress.ProgressOf(firstTask.Id) == 0,
            "quest repetible reinicia tareas y conserva historial de completado");
    }

    private static void VerifyInvalidCompletionAndFailure()
    {
        var definitions = new DefinitionRegistry();
        var task = new QuestTaskDefinition(Guid.NewGuid(), "Pendiente", quantity: 2);
        var quest = new QuestDefinition(
            DefinitionId.New(), new ContentKey("quests.runtime.failure"), "Quest Failure", string.Empty,
            true, 1, null, tasks: [task]);
        definitions.Register(quest);
        var systems = new GameSystems(definitions);
        var player = CreatePlayer();

        systems.Quests.Start(player, quest.Id);
        var rejected = false;
        try { systems.Quests.Complete(player, quest.Id); }
        catch (InvalidOperationException) { rejected = true; }
        Expect(rejected, "QuestSystem rechaza completar tareas pendientes");
        Expect(player.Quests.StateOf(quest.Id) == QuestRuntimeState.Active,
            "rechazo no corrompe estado de quest");

        systems.Quests.Fail(player, quest.Id);
        Expect(player.Quests.StateOf(quest.Id) == QuestRuntimeState.Failed,
            "FailQuest produce estado terminal propio");

        var failedCondition = new ConditionDefinition(
            ConditionKind.QuestState,
            references: new Dictionary<string, DefinitionId> { ["quest"] = quest.Id },
            text: new Dictionary<string, string> { ["state"] = "Failed" });
        Expect(systems.Conditions.Evaluate(player, failedCondition),
            "condiciones observan quest fallida");
    }

    private static void VerifyQuestPersistenceReconnect()
    {
        var map = new MapDefinition(
            DefinitionId.New(), new ContentKey("maps.quest_persistence"), "Quest persistence", string.Empty,
            true, 1, null, new ContentKey("maps.quest_persistence.visual"),
            new BoundsData(new(0, 0), new(320, 320)), new Vector2Data(100, 100), new Vector2IntData(32, 32));
        var task = new QuestTaskDefinition(Guid.NewGuid(), "Persistir progreso", quantity: 3);
        var quest = new QuestDefinition(
            DefinitionId.New(), new ContentKey("quests.runtime.persistence"), "Quest Persistence", string.Empty,
            true, 1, null, tasks: [task]);
        var definitions = new DefinitionRegistry();
        definitions.Register(quest);
        var systems = new GameSystems(definitions);
        var repository = new InMemoryCharacterRepository();
        var account = new AccountId(Guid.NewGuid());
        var record = repository.CreateAsync(account, "QuestReconnect", map.Id, map.Spawn, DefinitionId.Empty)
            .GetAwaiter().GetResult();
        var player = new Player(
            new EntityId(91_002), account, record.Id, new MapInstanceId(1), map.Spawn,
            new ContentKey("template.player"), record.Name);
        systems.Quests.Start(player, quest.Id);
        systems.Quests.Advance(player, quest.Id, task.Id, 2);

        var persistence = new PersistenceService(repository, map);
        persistence.SaveCharacterAsync(player).GetAwaiter().GetResult();
        var stored = repository.GetAsync(record.Id).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Quest persistence FAIL: record inexistente.");
        Expect(stored.QuestData.Contains(quest.Id.Value.ToString("D"), StringComparison.OrdinalIgnoreCase),
            "checkpoint serializa quest_data");

        var reconnected = new Player(
            new EntityId(91_003), account, record.Id, new MapInstanceId(1), map.Spawn,
            new ContentKey("template.player"), record.Name);
        persistence.RestoreQuests(reconnected, stored);
        Expect(reconnected.Quests.TryGet(quest.Id, out var restored) && restored is not null,
            "reconexión restaura journal");
        Expect(restored!.State == QuestRuntimeState.Active && restored.ProgressOf(task.Id) == 2,
            "reconexión conserva estado y progreso de tarea");
    }

    private static EventCommandDefinition QuestCommand(
        EventCommandKind kind,
        DefinitionId quest,
        Dictionary<string, float>? numbers = null,
        Dictionary<string, string>? text = null)
        => new(
            Guid.NewGuid(),
            kind,
            references: new Dictionary<string, DefinitionId> { ["quest"] = quest },
            numbers: numbers,
            text: text);

    private static Player CreatePlayer()
        => new(
            new EntityId(91_001),
            new AccountId(Guid.NewGuid()),
            new CharacterId(Guid.NewGuid()),
            new MapInstanceId(1),
            new Vector2Data(100, 100),
            new ContentKey("template.player"),
            "Quest Tester");

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("QuestRuntimeVerification FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
