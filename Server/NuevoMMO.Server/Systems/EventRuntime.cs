using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.Systems;

public sealed record EventExecutionResult(bool Success, string Message, IReadOnlyList<string> Log)
{
    public static EventExecutionResult Fail(string message) => new(false, message, []);
    public static EventExecutionResult Ok(IReadOnlyList<string> log) => new(true, string.Empty, log);
}

/// <summary>
/// Ejecutor autoritativo de EventDefinition. Elige la página activa por prioridad/condiciones
/// y delega cada mutación al sistema propietario cuando existe. Los módulos aún no implementados
/// permanecen explícitamente como deferred.
/// </summary>
public sealed class EventRuntime
{
    private const int MaximumTriggerDepth = 8;
    private readonly DefinitionRegistry definitions;
    private readonly ConditionSystem conditions;
    private readonly ProgressionSystem? progression;
    private readonly InventorySystem? inventory;
    private readonly LootSystem? loot;
    private readonly EffectSystem? effects;
    private readonly QuestSystem? quests;

    public EventRuntime(
        DefinitionRegistry definitions,
        ConditionSystem? conditions = null,
        ProgressionSystem? progression = null,
        InventorySystem? inventory = null,
        LootSystem? loot = null,
        EffectSystem? effects = null,
        QuestSystem? quests = null)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.conditions = conditions ?? new ConditionSystem();
        this.progression = progression;
        this.inventory = inventory;
        this.loot = loot;
        this.effects = effects;
        this.quests = quests;
    }

    public EventPageDefinition? ActivePage(EventDefinition definition, Player player)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(player);
        return definition.Pages
            .Where(page => conditions.Evaluate(player, page.Conditions, Context(player)))
            .OrderByDescending(static page => page.Priority)
            .FirstOrDefault();
    }

    public EventExecutionResult TryTrigger(
        Player player,
        EventDefinition definition,
        EventTrigger trigger,
        long nowMilliseconds)
        => TryTriggerCore(player, definition, trigger, nowMilliseconds, 0);

    private EventExecutionResult TryTriggerCore(
        Player player,
        EventDefinition definition,
        EventTrigger trigger,
        long nowMilliseconds,
        int depth)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(definition);
        if (depth > MaximumTriggerDepth)
            return EventExecutionResult.Fail("Cadena de eventos demasiado profunda.");
        if (!definition.Enabled) return EventExecutionResult.Fail("El evento está deshabilitado.");
        var page = ActivePage(definition, player);
        if (page is null) return EventExecutionResult.Fail("Ninguna página del evento está activa.");
        if (page.Trigger != trigger && trigger != EventTrigger.Custom)
            return EventExecutionResult.Fail("El trigger no corresponde a la página activa.");
        if (page.RootCommandListId == Guid.Empty ||
            !page.CommandLists.TryGetValue(page.RootCommandListId, out var commands))
            return EventExecutionResult.Ok([]);

        var log = new List<string>();
        foreach (var command in commands)
            ExecuteCommand(player, command, log, nowMilliseconds, depth);
        return EventExecutionResult.Ok(log);
    }

    public IReadOnlyList<EventDefinition> MapEvents(DefinitionId mapId)
        => definitions.GetAll<EventDefinition>()
            .Where(evt => evt.Enabled && evt.Scope == EventScope.Map && evt.Placement?.MapId == mapId)
            .ToArray();

    /// <summary>
    /// Pulso por tick: MapEnter (una vez), Autorun (una vez por página), PlayerTouch al entrar al radio y Timer.
    /// </summary>
    public IReadOnlyList<EventExecutionResult> Pulse(Player player, MapDefinition map, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(map);
        var results = new List<EventExecutionResult>();
        foreach (var evt in MapEvents(map.Id))
        {
            var page = ActivePage(evt, player);
            if (page is null) continue;

            if (page.Trigger == EventTrigger.MapEnter &&
                player.Events.ConsumedTriggers.Add($"mapenter:{evt.Id.Value:N}"))
                results.Add(TryTrigger(player, evt, EventTrigger.MapEnter, nowMilliseconds));

            if (page.Trigger == EventTrigger.Autorun)
            {
                var autorunKey = $"autorun:{evt.Id.Value:N}:{page.Id:N}";
                if (player.Events.ConsumedTriggers.Add(autorunKey))
                    results.Add(TryTrigger(player, evt, EventTrigger.Autorun, nowMilliseconds));
            }

            if (evt.Placement is { } placement)
            {
                var inside = player.Position.DistanceSquaredTo(placement.Position) <= page.InteractionRadius * page.InteractionRadius;
                if (page.Trigger == EventTrigger.PlayerTouch)
                {
                    if (inside && player.Events.TouchingEvents.Add(evt.Id.Value))
                        results.Add(TryTrigger(player, evt, EventTrigger.PlayerTouch, nowMilliseconds));
                    else if (!inside)
                        player.Events.TouchingEvents.Remove(evt.Id.Value);
                }

                if (page.Trigger == EventTrigger.Timer)
                {
                    var interval = Math.Max(1, (int)page.TriggerParameters.GetValueOrDefault("intervalMilliseconds", 1000));
                    var last = player.Events.LastTimerFire.GetValueOrDefault(page.Id, 0);
                    if (inside && nowMilliseconds - last >= interval)
                    {
                        player.Events.LastTimerFire[page.Id] = nowMilliseconds;
                        results.Add(TryTrigger(player, evt, EventTrigger.Timer, nowMilliseconds));
                    }
                }
            }
        }

        return results;
    }

    public IReadOnlyList<EventExecutionResult> NotifyEntityDefeated(Player player, Entity defeated, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(defeated);
        var results = new List<EventExecutionResult>();
        foreach (var evt in definitions.GetAll<EventDefinition>().Where(static value => value.Enabled))
        {
            var page = ActivePage(evt, player);
            if (page is null || page.Trigger != EventTrigger.EntityDeath) continue;
            results.Add(TryTrigger(player, evt, EventTrigger.EntityDeath, nowMilliseconds));
        }

        return results;
    }

    private void ExecuteCommand(
        Player player,
        EventCommandDefinition command,
        List<string> log,
        long nowMilliseconds,
        int depth)
    {
        if (command.Condition is { } condition && !conditions.Evaluate(player, condition, Context(player)))
            return;

        switch (command.Kind)
        {
            case EventCommandKind.Dialogue:
            case EventCommandKind.ShowNotification:
                if (command.Text.TryGetValue("text", out var text) && !string.IsNullOrWhiteSpace(text))
                    log.Add(text);
                break;

            case EventCommandKind.SetSwitch:
                if (command.Text.TryGetValue("key", out var switchKey))
                    player.Events.SetSwitch(switchKey, command.Numbers.GetValueOrDefault("value", 1) != 0);
                break;

            case EventCommandKind.SetVariable:
                if (command.Text.TryGetValue("key", out var variableKey))
                    player.Events.SetVariable(variableKey, command.Numbers.GetValueOrDefault("value"));
                break;

            case EventCommandKind.GiveExperience:
            {
                if (progression is null)
                {
                    log.Add("deferred:GiveExperience");
                    break;
                }
                var amount = ReadNonNegativeLong(command, "amount");
                if (amount > 0) progression.GrantExperience(player, amount);
                log.Add($"xp:{amount}");
                break;
            }

            case EventCommandKind.LearnTechnique:
                if (command.References.TryGetValue("technique", out var techniqueId))
                {
                    player.Techniques.Learn(techniqueId);
                    log.Add($"learn:{techniqueId.Value:N}");
                }
                break;

            case EventCommandKind.ForgetTechnique:
                if (command.References.TryGetValue("technique", out var forgetId))
                {
                    player.Techniques.Forget(forgetId);
                    log.Add($"forget:{forgetId.Value:N}");
                }
                break;

            case EventCommandKind.GiveItem:
            {
                if (inventory is null || loot is null || !command.References.TryGetValue("item", out var itemId))
                {
                    log.Add("deferred:GiveItem");
                    break;
                }
                var quantity = ReadPositiveInt(command, "quantity", 1);
                var item = loot.CreateItem(itemId, quantity);
                if (inventory.TryGive(player.Inventory, item, out _, out var error))
                    log.Add($"give_item:{itemId.Value:N}:{quantity}");
                else
                    log.Add($"give_item_failed:{error}");
                break;
            }

            case EventCommandKind.TakeItem:
            {
                if (inventory is null || !command.References.TryGetValue("item", out var itemId))
                {
                    log.Add("deferred:TakeItem");
                    break;
                }
                var quantity = ReadPositiveInt(command, "quantity", 1);
                if (inventory.CanTake(player.Inventory, itemId, quantity))
                {
                    inventory.Take(player.Inventory, itemId, quantity);
                    log.Add($"take_item:{itemId.Value:N}:{quantity}");
                }
                else
                    log.Add($"take_item_failed:{itemId.Value:N}:{quantity}");
                break;
            }

            case EventCommandKind.ApplyEffect:
            {
                if (effects is null || !command.References.TryGetValue("effect", out var effectId))
                {
                    log.Add("deferred:ApplyEffect");
                    break;
                }
                effects.Apply(player, effectId, player.Id, nowMilliseconds);
                log.Add($"apply_effect:{effectId.Value:N}");
                break;
            }

            case EventCommandKind.RemoveEffect:
            {
                if (effects is null || !command.References.TryGetValue("effect", out var effectId))
                {
                    log.Add("deferred:RemoveEffect");
                    break;
                }
                var removed = effects.Remove(player, effectId, requireDispellable: false, nowMilliseconds);
                log.Add(removed ? $"remove_effect:{effectId.Value:N}" : $"remove_effect_missing:{effectId.Value:N}");
                break;
            }

            case EventCommandKind.StartQuest:
            {
                if (quests is null)
                {
                    log.Add("deferred:StartQuest");
                    break;
                }
                var questId = RequiredCommandReference(command, "quest");
                quests.Start(player, questId, Context(player));
                log.Add($"quest_started:{questId.Value:N}");
                break;
            }

            case EventCommandKind.AdvanceQuest:
            {
                if (quests is null)
                {
                    log.Add("deferred:AdvanceQuest");
                    break;
                }
                var questId = RequiredCommandReference(command, "quest");
                var taskId = ReadOptionalTaskId(command);
                var amount = ReadPositiveInt(command, "amount", 1);
                quests.Advance(player, questId, taskId, amount, Context(player));
                log.Add($"quest_advanced:{questId.Value:N}:{amount}");
                break;
            }

            case EventCommandKind.CompleteQuest:
            {
                if (quests is null)
                {
                    log.Add("deferred:CompleteQuest");
                    break;
                }
                var questId = RequiredCommandReference(command, "quest");
                quests.Complete(player, questId, Context(player));
                log.Add($"quest_completed:{questId.Value:N}");
                break;
            }

            case EventCommandKind.FailQuest:
            {
                if (quests is null)
                {
                    log.Add("deferred:FailQuest");
                    break;
                }
                var questId = RequiredCommandReference(command, "quest");
                quests.Fail(player, questId);
                log.Add($"quest_failed:{questId.Value:N}");
                break;
            }

            case EventCommandKind.TriggerEvent:
            {
                if (!command.References.TryGetValue("event", out var eventId) ||
                    !definitions.TryGet<EventDefinition>(eventId, out var nested) || nested is null)
                {
                    log.Add("trigger_event_missing");
                    break;
                }
                var nestedResult = TryTriggerCore(player, nested, EventTrigger.Custom, nowMilliseconds, depth + 1);
                if (nestedResult.Success) log.AddRange(nestedResult.Log);
                else log.Add($"trigger_event_failed:{nestedResult.Message}");
                break;
            }

            case EventCommandKind.Wait:
                // Requiere un scheduler de secuencias; no debe bloquear el tick del servidor.
                log.Add("deferred:Wait");
                break;

            case EventCommandKind.Teleport:
                // El teletransporte dentro del mapa actual ya es seguro. Cambiar de MapDefinition/instancia
                // pertenece al WorldRuntime y se conserva diferido hasta exponer una operación propietaria al EventRuntime.
                if (command.References.ContainsKey("map"))
                {
                    log.Add("deferred:Teleport");
                    break;
                }
                if (command.DestinationOrDefault() is { } destination)
                    player.MoveTo(destination, default);
                break;

            case EventCommandKind.PlaySound:
            case EventCommandKind.PlayAnimation:
                log.Add($"{command.Kind}:{command.Text.GetValueOrDefault("key", command.Kind.ToString())}");
                break;

            case EventCommandKind.MoveEntity:
            case EventCommandKind.SpawnEntity:
            case EventCommandKind.DespawnEntity:
            case EventCommandKind.OpenShop:
            case EventCommandKind.OpenBank:
            case EventCommandKind.SetCheckpoint:
            case EventCommandKind.ChangeTradition:
            case EventCommandKind.ModifyProfession:
            case EventCommandKind.Choice:
            case EventCommandKind.Conditional:
            case EventCommandKind.Custom:
                log.Add($"deferred:{command.Kind}");
                break;

            default:
                log.Add($"deferred:{command.Kind}");
                break;
        }
    }

    private static DefinitionId RequiredCommandReference(EventCommandDefinition command, string key)
        => command.References.TryGetValue(key, out var value) && !value.IsEmpty
            ? value
            : throw new InvalidDataException($"{command.Kind} requiere reference '{key}'.");

    private static Guid? ReadOptionalTaskId(EventCommandDefinition command)
    {
        if (!command.Text.TryGetValue("task", out var raw) || string.IsNullOrWhiteSpace(raw)) return null;
        return Guid.TryParse(raw, out var taskId) && taskId != Guid.Empty
            ? taskId
            : throw new InvalidDataException("AdvanceQuest.task debe ser un Guid válido.");
    }

    private static int ReadPositiveInt(EventCommandDefinition command, string key, int fallback)
    {
        var value = command.Numbers.GetValueOrDefault(key, fallback);
        if (value < 1 || value > int.MaxValue)
            throw new InvalidDataException($"{command.Kind}.{key} debe ser un entero positivo.");
        var rounded = MathF.Round(value, MidpointRounding.AwayFromZero);
        if (MathF.Abs(rounded - value) > 0.001f)
            throw new InvalidDataException($"{command.Kind}.{key} debe ser entero.");
        return checked((int)rounded);
    }

    private static long ReadNonNegativeLong(EventCommandDefinition command, string key)
    {
        var value = command.Numbers.GetValueOrDefault(key);
        if (value < 0 || value > long.MaxValue)
            throw new InvalidDataException($"{command.Kind}.{key} debe ser no negativo.");
        var rounded = Math.Round((double)value, MidpointRounding.AwayFromZero);
        if (Math.Abs(rounded - value) > 0.001d)
            throw new InvalidDataException($"{command.Kind}.{key} debe ser entero.");
        return checked((long)rounded);
    }

    private static ConditionEvaluationContext Context(Player player)
        => new(
            Variables: player.Events.Variables,
            Switches: player.Events.Switches);
}

file static class EventCommandDestination
{
    public static Vector2Data? DestinationOrDefault(this EventCommandDefinition command)
    {
        if (!command.Numbers.TryGetValue("x", out var x) || !command.Numbers.TryGetValue("y", out var y))
            return null;
        return new Vector2Data(x, y);
    }
}
