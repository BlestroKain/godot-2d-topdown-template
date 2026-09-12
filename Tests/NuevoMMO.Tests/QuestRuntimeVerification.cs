using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;

internal static class QuestRuntimeVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyEventOwnedQuestLifecycle();
        VerifyInvalidCompletionAndFailure();
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
