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
/// y corre la lista raíz de comandos. No abre UI; emite texto/notificaciones para que el handler las envíe.
/// </summary>
public sealed class EventRuntime(DefinitionRegistry definitions, ConditionSystem? conditions = null)
{
    private readonly ConditionSystem conditions = conditions ?? new ConditionSystem();

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
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(definition);
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
            ExecuteCommand(player, command, log, nowMilliseconds);
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

    private void ExecuteCommand(Player player, EventCommandDefinition command, List<string> log, long nowMilliseconds)
    {
        _ = nowMilliseconds;
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
                log.Add($"xp:{command.Numbers.GetValueOrDefault("amount")}");
                break;
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
                log.Add($"give_item:{command.References.GetValueOrDefault("item")}");
                break;
            case EventCommandKind.TakeItem:
                log.Add($"take_item:{command.References.GetValueOrDefault("item")}");
                break;
            case EventCommandKind.Wait:
                break;
            case EventCommandKind.Teleport:
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
            case EventCommandKind.ApplyEffect:
            case EventCommandKind.RemoveEffect:
            case EventCommandKind.StartQuest:
            case EventCommandKind.AdvanceQuest:
            case EventCommandKind.CompleteQuest:
            case EventCommandKind.OpenShop:
            case EventCommandKind.OpenBank:
            case EventCommandKind.SetCheckpoint:
            case EventCommandKind.TriggerEvent:
            case EventCommandKind.ChangeTradition:
            case EventCommandKind.ModifyProfession:
            case EventCommandKind.Custom:
                log.Add($"deferred:{command.Kind}");
                break;
            default:
                log.Add($"deferred:{command.Kind}");
                break;
        }
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
