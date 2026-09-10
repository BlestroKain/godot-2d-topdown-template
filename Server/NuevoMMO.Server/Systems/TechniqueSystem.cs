using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public enum TechniqueUseFailure : byte
{
    None,
    CasterUnavailable,
    UnknownTechnique,
    TechniqueDisabled,
    AlreadyCasting,
    Cooldown,
    RequirementsUnavailable,
    RequirementsNotMet,
    InvalidTarget,
    DifferentMap,
    OutOfRange,
    LineOfSightBlocked,
    InsufficientVital,
    ResourceSystemUnavailable,
    InsufficientResource
}

public enum TechniqueActionExecutionStatus : byte
{
    Executed,
    Deferred,
    Skipped
}

public readonly record struct TechniqueActionExecution(
    TechniqueActionDefinition Action,
    TechniqueActionExecutionStatus Status,
    EntityId? TargetId,
    DamageResult? Damage = null,
    HealingResult? Healing = null,
    EffectApplyResult? Effect = null,
    string Message = "");

public sealed record TechniqueUseResult(
    bool Success,
    TechniqueUseFailure Failure,
    string Message,
    DefinitionId TechniqueId,
    long CompletesAtMilliseconds,
    bool Channeling,
    IReadOnlyList<TechniqueActionExecution> Actions)
{
    public static TechniqueUseResult Fail(TechniqueUseFailure failure, string message, DefinitionId techniqueId)
        => new(false, failure, message, techniqueId, 0, false, []);
}

public sealed record TechniquePulseResult(
    EntityId CasterId,
    DefinitionId TechniqueId,
    TechniqueActionMoment Moment,
    IReadOnlyList<TechniqueActionExecution> Actions,
    bool Finished);

/// <summary>
/// Adaptador para recursos propios de Tradiciones. El sistema básico ya soporta costes de HP/PM;
/// recursos adicionales se conectan aquí sin meter nombres concretos en Player ni TechniqueSystem.
/// </summary>
public interface ITechniqueResourceAccess
{
    bool CanPay(Player player, IReadOnlyDictionary<string, float> costs, out string error);
    void Pay(Player player, IReadOnlyDictionary<string, float> costs);
}

/// <summary>
/// Ejecución autoritativa básica de técnicas: aprendizaje, cooldown, coste, target/rango,
/// cast, channel y acciones inmediatas de combate/efecto. Las acciones espaciales o de eventos
/// se devuelven como Deferred para que su sistema dueño las procese.
/// </summary>
public sealed class TechniqueSystem
{
    private sealed class ActiveCast
    {
        public required Player Caster { get; init; }
        public required TechniqueDefinition Definition { get; init; }
        public LivingEntity? Target { get; init; }
        public Vector2Data? Point { get; init; }
        public required long StartedAtMilliseconds { get; init; }
        public required long CompletesAtMilliseconds { get; init; }
        public bool ImpactExecuted { get; set; }
        public long ChannelEndsAtMilliseconds { get; set; }
        public long NextChannelTickMilliseconds { get; set; }
    }

    private readonly DefinitionRegistry definitions;
    private readonly CombatSystem combat;
    private readonly EffectSystem effects;
    private readonly ITechniqueResourceAccess? resources;
    private readonly Func<Player, ConditionGroupDefinition, bool>? requirementsEvaluator;
    private readonly Func<Entity, Vector2Data, bool>? lineOfSight;
    private readonly Dictionary<EntityId, ActiveCast> casts = [];

    public TechniqueSystem(
        DefinitionRegistry definitions,
        CombatSystem combat,
        EffectSystem effects,
        ITechniqueResourceAccess? resources = null,
        Func<Player, ConditionGroupDefinition, bool>? requirementsEvaluator = null,
        Func<Entity, Vector2Data, bool>? lineOfSight = null)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
        this.effects = effects ?? throw new ArgumentNullException(nameof(effects));
        this.resources = resources;
        this.requirementsEvaluator = requirementsEvaluator;
        this.lineOfSight = lineOfSight;
    }

    public bool IsCasting(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return casts.ContainsKey(player.Id);
    }

    public TechniqueUseResult BeginUse(
        Player caster,
        DefinitionId techniqueId,
        LivingEntity? target,
        Vector2Data? point,
        long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(caster);
        if (techniqueId.IsEmpty) throw new ArgumentException("TechniqueId vacío.", nameof(techniqueId));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));

        if (!caster.IsAlive)
            return TechniqueUseResult.Fail(TechniqueUseFailure.CasterUnavailable, "El personaje no puede usar técnicas en este estado.", techniqueId);
        if (!caster.Techniques.Knows(techniqueId))
            return TechniqueUseResult.Fail(TechniqueUseFailure.UnknownTechnique, "La técnica no está aprendida.", techniqueId);
        if (!definitions.TryGet<TechniqueDefinition>(techniqueId, out var definition) || definition is null)
            return TechniqueUseResult.Fail(TechniqueUseFailure.UnknownTechnique, "TechniqueDefinition inexistente.", techniqueId);
        if (!definition.Enabled)
            return TechniqueUseResult.Fail(TechniqueUseFailure.TechniqueDisabled, "La técnica está deshabilitada.", techniqueId);
        if (casts.ContainsKey(caster.Id))
            return TechniqueUseResult.Fail(TechniqueUseFailure.AlreadyCasting, "Ya hay una técnica en ejecución.", techniqueId);

        caster.Techniques.ClearExpiredCooldowns(nowMilliseconds);
        if (!caster.Techniques.IsReady(definition, nowMilliseconds))
            return TechniqueUseResult.Fail(
                TechniqueUseFailure.Cooldown,
                $"Técnica en cooldown ({caster.Techniques.RemainingCooldown(definition, nowMilliseconds)} ms).",
                techniqueId);

        var validation = ValidateUse(caster, definition, target, point);
        if (!validation.Success) return validation;

        var completesAt = checked(nowMilliseconds + definition.Timing.CastMilliseconds);
        var active = new ActiveCast
        {
            Caster = caster,
            Definition = definition,
            Target = ResolveTarget(caster, definition, target),
            Point = point,
            StartedAtMilliseconds = nowMilliseconds,
            CompletesAtMilliseconds = completesAt
        };

        var startActions = ExecuteMoment(active, TechniqueActionMoment.CastStart, nowMilliseconds);
        if (definition.Timing.CastMilliseconds > 0)
        {
            casts.Add(caster.Id, active);
            return new TechniqueUseResult(true, TechniqueUseFailure.None, string.Empty, techniqueId,
                completesAt, false, startActions);
        }

        var completion = CompleteImpact(active, nowMilliseconds);
        var combined = startActions.Concat(completion.Actions).ToArray();
        if (completion.Finished)
            return new TechniqueUseResult(true, TechniqueUseFailure.None, string.Empty, techniqueId,
                nowMilliseconds, false, combined);

        casts.Add(caster.Id, active);
        return new TechniqueUseResult(true, TechniqueUseFailure.None, string.Empty, techniqueId,
            nowMilliseconds, true, combined);
    }

    /// <summary>
    /// Avanza casts y channels pendientes. Debe llamarse desde el tick autoritativo.
    /// </summary>
    public IReadOnlyList<TechniquePulseResult> Advance(long nowMilliseconds)
    {
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (casts.Count == 0) return [];

        var pulses = new List<TechniquePulseResult>();
        foreach (var pair in casts.ToArray())
        {
            var active = pair.Value;
            if (!active.Caster.IsAlive)
            {
                casts.Remove(pair.Key);
                continue;
            }

            if (!active.ImpactExecuted && nowMilliseconds >= active.CompletesAtMilliseconds)
            {
                var validation = ValidateUse(active.Caster, active.Definition, active.Target, active.Point, checkCooldown: false);
                if (!validation.Success || !CanPay(active.Caster, active.Definition, out _))
                {
                    casts.Remove(pair.Key);
                    continue;
                }

                var completed = CompleteImpact(active, nowMilliseconds);
                pulses.Add(completed);
                if (completed.Finished)
                {
                    casts.Remove(pair.Key);
                    continue;
                }
            }

            if (!active.ImpactExecuted || active.ChannelEndsAtMilliseconds <= 0) continue;
            var interval = active.Definition.Timing.TickIntervalMilliseconds;
            if (interval > 0)
            {
                var limit = Math.Min(nowMilliseconds, active.ChannelEndsAtMilliseconds);
                while (active.NextChannelTickMilliseconds > 0 && active.NextChannelTickMilliseconds <= limit)
                {
                    var actions = ExecuteMoment(active, TechniqueActionMoment.Tick, active.NextChannelTickMilliseconds);
                    pulses.Add(new(active.Caster.Id, active.Definition.Id, TechniqueActionMoment.Tick, actions, false));
                    active.NextChannelTickMilliseconds = checked(active.NextChannelTickMilliseconds + interval);
                }
            }

            if (nowMilliseconds >= active.ChannelEndsAtMilliseconds)
            {
                var actions = ExecuteMoment(active, TechniqueActionMoment.Expire, active.ChannelEndsAtMilliseconds);
                pulses.Add(new(active.Caster.Id, active.Definition.Id, TechniqueActionMoment.Expire, actions, true));
                casts.Remove(pair.Key);
            }
        }
        return pulses;
    }

    public bool Cancel(EntityId casterId)
    {
        if (casterId.Value <= 0) return false;
        return casts.Remove(casterId);
    }

    /// <summary>
    /// Ejecuta las acciones producidas por un EffectSystem contra el portador del efecto.
    /// </summary>
    public IReadOnlyList<TechniqueActionExecution> ExecuteEffectActions(
        LivingEntity? source,
        LivingEntity target,
        IReadOnlyList<TechniqueActionDefinition> actions,
        long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(actions);
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        var result = new List<TechniqueActionExecution>(actions.Count);
        foreach (var action in actions)
            result.AddRange(ExecuteAction(source, target, action, nowMilliseconds, 0));
        return result;
    }

    private TechniqueUseResult ValidateUse(
        Player caster,
        TechniqueDefinition definition,
        LivingEntity? target,
        Vector2Data? point,
        bool checkCooldown = true)
    {
        if (checkCooldown && !caster.Techniques.IsReady(definition, Environment.TickCount64))
        {
            // BeginUse hace la comprobación con el reloj explícito. Esta rama solo protege llamadas internas.
        }

        if (!IsEmpty(definition.CastRequirements))
        {
            if (requirementsEvaluator is null)
                return TechniqueUseResult.Fail(TechniqueUseFailure.RequirementsUnavailable,
                    "La técnica tiene requisitos pero no hay evaluador de condiciones configurado.", definition.Id);
            if (!requirementsEvaluator(caster, definition.CastRequirements))
                return TechniqueUseResult.Fail(TechniqueUseFailure.RequirementsNotMet,
                    string.IsNullOrWhiteSpace(definition.CannotCastMessage) ? "No cumple los requisitos de la técnica." : definition.CannotCastMessage,
                    definition.Id);
        }

        var resolvedTarget = ResolveTarget(caster, definition, target);
        switch (definition.Targeting.Mode)
        {
            case TechniqueTargetMode.Self:
                break;
            case TechniqueTargetMode.Entity when resolvedTarget is null:
                return TechniqueUseResult.Fail(TechniqueUseFailure.InvalidTarget, "La técnica requiere un objetivo.", definition.Id);
            case TechniqueTargetMode.Point or TechniqueTargetMode.Area or TechniqueTargetMode.Cone or TechniqueTargetMode.Line
                when point is null && !definition.Targeting.AllowEmptyPoint:
                return TechniqueUseResult.Fail(TechniqueUseFailure.InvalidTarget, "La técnica requiere un punto objetivo.", definition.Id);
        }

        if (resolvedTarget is not null)
        {
            if (!resolvedTarget.IsAlive)
                return TechniqueUseResult.Fail(TechniqueUseFailure.InvalidTarget, "El objetivo no está disponible.", definition.Id);
            if (resolvedTarget.MapInstanceId != caster.MapInstanceId)
                return TechniqueUseResult.Fail(TechniqueUseFailure.DifferentMap, "El objetivo está en otra instancia.", definition.Id);
            var relation = Relation(caster, resolvedTarget);
            if ((definition.Targeting.Relations & relation) == 0)
                return TechniqueUseResult.Fail(TechniqueUseFailure.InvalidTarget, "La relación del objetivo no es válida para esta técnica.", definition.Id);
        }

        var targetPoint = resolvedTarget?.Position ?? point;
        if (targetPoint is { } destination)
        {
            if (!destination.IsFinite)
                return TechniqueUseResult.Fail(TechniqueUseFailure.InvalidTarget, "Punto objetivo inválido.", definition.Id);
            if (definition.Targeting.Range > 0 && caster.Position.DistanceTo(destination) > definition.Targeting.Range)
                return TechniqueUseResult.Fail(TechniqueUseFailure.OutOfRange, "Objetivo fuera de alcance.", definition.Id);
            if (definition.Targeting.RequiresLineOfSight && lineOfSight is not null && !lineOfSight(caster, destination))
                return TechniqueUseResult.Fail(TechniqueUseFailure.LineOfSightBlocked, "No hay línea de visión.", definition.Id);
        }

        if (!CanPay(caster, definition, out var paymentError))
            return TechniqueUseResult.Fail(paymentError.failure, paymentError.message, definition.Id);

        return new TechniqueUseResult(true, TechniqueUseFailure.None, string.Empty, definition.Id, 0, false, []);
    }

    private TechniquePulseResult CompleteImpact(ActiveCast active, long nowMilliseconds)
    {
        Pay(active.Caster, active.Definition);
        active.ImpactExecuted = true;

        var actions = new List<TechniqueActionExecution>();
        actions.AddRange(ExecuteMoment(active, TechniqueActionMoment.CastComplete, nowMilliseconds));
        actions.AddRange(ExecuteMoment(active, TechniqueActionMoment.Impact, nowMilliseconds));

        var cooldown = active.Definition.Timing.CooldownMilliseconds;
        var globalCooldown = ReadMilliseconds(active.Definition.Parameters, "globalCooldownMilliseconds");
        active.Caster.Techniques.StartCooldown(active.Definition, nowMilliseconds, cooldown, globalCooldown);

        if (active.Definition.Timing.ChannelMilliseconds <= 0)
            return new(active.Caster.Id, active.Definition.Id, TechniqueActionMoment.Impact, actions, true);

        active.ChannelEndsAtMilliseconds = checked(nowMilliseconds + active.Definition.Timing.ChannelMilliseconds);
        active.NextChannelTickMilliseconds = active.Definition.Timing.TickIntervalMilliseconds > 0
            ? checked(nowMilliseconds + active.Definition.Timing.TickIntervalMilliseconds)
            : 0;
        return new(active.Caster.Id, active.Definition.Id, TechniqueActionMoment.Impact, actions, false);
    }

    private IReadOnlyList<TechniqueActionExecution> ExecuteMoment(
        ActiveCast active,
        TechniqueActionMoment moment,
        long nowMilliseconds)
    {
        var result = new List<TechniqueActionExecution>();
        foreach (var action in active.Definition.Actions.Where(action => action.Moment == moment))
            result.AddRange(ExecuteAction(active.Caster, active.Target, action, nowMilliseconds, 0));
        return result;
    }

    private IReadOnlyList<TechniqueActionExecution> ExecuteAction(
        LivingEntity? source,
        LivingEntity? target,
        TechniqueActionDefinition action,
        long nowMilliseconds,
        int depth)
    {
        if (depth > 8) throw new InvalidOperationException("Cadena de acciones de efecto demasiado profunda.");
        var result = new List<TechniqueActionExecution>();

        switch (action.Kind)
        {
            case TechniqueActionKind.Damage:
            {
                if (target is null)
                {
                    result.Add(new(action, TechniqueActionExecutionStatus.Skipped, null, Message: "Damage requiere objetivo."));
                    break;
                }
                var stats = source?.Stats ?? new StatBlock();
                var raw = CombatSystem.CalculateScaledAmount(action.Amount, action.Scaling, stats);
                var damage = combat.ApplyDamage(source, target, raw, action.Element);
                result.Add(new(action, TechniqueActionExecutionStatus.Executed, target.Id, Damage: damage));
                break;
            }
            case TechniqueActionKind.Heal:
            {
                if (target is null)
                {
                    result.Add(new(action, TechniqueActionExecutionStatus.Skipped, null, Message: "Heal requiere objetivo."));
                    break;
                }
                var stats = source?.Stats ?? new StatBlock();
                var raw = CombatSystem.CalculateScaledAmount(action.Amount, action.Scaling, stats);
                if (raw > int.MaxValue) throw new OverflowException("Curación fuera de rango.");
                var amount = Math.Max(0, (int)MathF.Round(raw, MidpointRounding.AwayFromZero));
                var healing = combat.ApplyHealing(source, target, amount);
                result.Add(new(action, TechniqueActionExecutionStatus.Executed, target.Id, Healing: healing));
                break;
            }
            case TechniqueActionKind.ApplyEffect:
            {
                if (target is null || action.EffectId is not { } effectId)
                {
                    result.Add(new(action, TechniqueActionExecutionStatus.Skipped, target?.Id, Message: "ApplyEffect requiere objetivo y EffectId."));
                    break;
                }
                var applied = effects.Apply(target, effectId, source?.Id, nowMilliseconds);
                result.Add(new(action, TechniqueActionExecutionStatus.Executed, target.Id, Effect: applied));
                foreach (var nested in applied.TriggeredActions)
                    result.AddRange(ExecuteAction(source, target, nested, nowMilliseconds, depth + 1));
                break;
            }
            case TechniqueActionKind.RemoveEffect:
            {
                if (target is null || action.EffectId is not { } effectId)
                {
                    result.Add(new(action, TechniqueActionExecutionStatus.Skipped, target?.Id, Message: "RemoveEffect requiere objetivo y EffectId."));
                    break;
                }
                var removed = effects.Remove(target, effectId, requireDispellable: false, nowMilliseconds);
                result.Add(new(action, removed ? TechniqueActionExecutionStatus.Executed : TechniqueActionExecutionStatus.Skipped,
                    target.Id, Message: removed ? string.Empty : "El objetivo no tenía el efecto."));
                break;
            }
            default:
                result.Add(new(action, TechniqueActionExecutionStatus.Deferred, target?.Id,
                    Message: "La acción pertenece a otro sistema runtime."));
                break;
        }

        return result;
    }

    private bool CanPay(Player caster, TechniqueDefinition definition,
        out (TechniqueUseFailure failure, string message) error)
    {
        foreach (var cost in definition.VitalCosts)
        {
            if (!float.IsFinite(cost.Value) || cost.Value < 0 || cost.Value > int.MaxValue)
                throw new InvalidOperationException($"Coste vital inválido en {definition.Key}.");
            var value = (int)MathF.Ceiling(cost.Value);
            var available = cost.Key switch
            {
                VitalId.Health => caster.Health,
                VitalId.Mana => caster.Mana,
                _ => 0
            };
            if (available < value)
            {
                error = (TechniqueUseFailure.InsufficientVital, $"Vital insuficiente: {cost.Key}.");
                return false;
            }
        }

        if (definition.ResourceCosts.Count > 0)
        {
            if (resources is null)
            {
                error = (TechniqueUseFailure.ResourceSystemUnavailable,
                    "La técnica usa un recurso de Tradición que todavía no está conectado al runtime.");
                return false;
            }
            if (!resources.CanPay(caster, definition.ResourceCosts, out var resourceError))
            {
                error = (TechniqueUseFailure.InsufficientResource, resourceError);
                return false;
            }
        }

        error = default;
        return true;
    }

    private void Pay(Player caster, TechniqueDefinition definition)
    {
        foreach (var cost in definition.VitalCosts)
        {
            var value = (int)MathF.Ceiling(cost.Value);
            switch (cost.Key)
            {
                case VitalId.Health:
                    caster.TakeDamage(value);
                    break;
                case VitalId.Mana:
                    caster.ConsumeMana(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cost.Key), cost.Key, null);
            }
        }
        if (definition.ResourceCosts.Count > 0)
            resources!.Pay(caster, definition.ResourceCosts);
    }

    private static LivingEntity? ResolveTarget(Player caster, TechniqueDefinition definition, LivingEntity? target)
        => definition.Targeting.Mode == TechniqueTargetMode.Self ? caster : target;

    private static TechniqueTargetRelation Relation(Player caster, LivingEntity target)
    {
        if (target.Id == caster.Id) return TechniqueTargetRelation.Self;
        if (target is Mob) return TechniqueTargetRelation.Enemy;
        return TechniqueTargetRelation.Neutral;
    }

    private static bool IsEmpty(ConditionGroupDefinition group)
        => group.Conditions.Length == 0 && group.Groups.All(IsEmpty);

    private static int ReadMilliseconds(IReadOnlyDictionary<string, float> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value)) return 0;
        if (!float.IsFinite(value) || value < 0 || value > int.MaxValue)
            throw new InvalidOperationException($"Parámetro {key} inválido.");
        return (int)MathF.Round(value, MidpointRounding.AwayFromZero);
    }
}
