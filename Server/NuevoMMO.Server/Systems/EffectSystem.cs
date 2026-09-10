using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public enum EffectApplyStatus : byte
{
    Applied,
    Refreshed,
    Stacked,
    Replaced,
    Ignored
}

public readonly record struct EffectPulse(
    DefinitionId EffectId,
    EntityId? SourceId,
    int Stacks,
    TechniqueActionMoment Moment,
    IReadOnlyList<TechniqueActionDefinition> Actions);

public readonly record struct EffectApplyResult(
    EffectApplyStatus Status,
    DefinitionId EffectId,
    int Stacks,
    IReadOnlyList<TechniqueActionDefinition> TriggeredActions);

/// <summary>
/// Runtime autoritativo de buffs/debuffs. EffectDefinition describe la política; este sistema conserva
/// source, stacks, expiración y tick sin acoplar las acciones concretas a CombatSystem.
/// </summary>
public sealed class EffectSystem
{
    private sealed class ActiveEffect
    {
        public required long InstanceId { get; init; }
        public required DefinitionId DefinitionId { get; init; }
        public EntityId? SourceId { get; init; }
        public required long AppliedAtMilliseconds { get; init; }
        public long ExpiresAtMilliseconds { get; set; }
        public long NextTickAtMilliseconds { get; set; }
        public int Stacks { get; set; } = 1;
    }

    private readonly DefinitionRegistry definitions;
    private readonly Dictionary<EntityId, List<ActiveEffect>> active = [];
    private long nextInstanceId;

    public EffectSystem(DefinitionRegistry definitions)
        => this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));

    public int Count(LivingEntity target)
        => active.TryGetValue(target.Id, out var effects) ? effects.Count : 0;

    public bool Has(LivingEntity target, DefinitionId effectId)
        => active.TryGetValue(target.Id, out var effects) && effects.Any(effect => effect.DefinitionId == effectId);

    public EffectApplyResult Apply(LivingEntity target, DefinitionId effectId, EntityId? sourceId, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (effectId.IsEmpty) throw new ArgumentException("EffectId vacío.", nameof(effectId));
        if (sourceId is { } source && source.Value <= 0) throw new ArgumentException("SourceId inválido.", nameof(sourceId));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));

        var definition = definitions.Get<EffectDefinition>(effectId);
        if (!definition.Enabled) throw new InvalidOperationException("El efecto está deshabilitado.");

        if (!active.TryGetValue(target.Id, out var effects)) active[target.Id] = effects = [];
        var existing = effects.Where(effect => effect.DefinitionId == effectId).ToArray();
        var lifecycle = definition.Lifecycle;
        EffectApplyStatus status;
        ActiveEffect current;

        switch (lifecycle.StackPolicy)
        {
            case EffectStackPolicy.IgnoreIfPresent when existing.Length > 0:
                SynchronizeProjection(target, nowMilliseconds);
                return new(EffectApplyStatus.Ignored, effectId, existing[0].Stacks, []);

            case EffectStackPolicy.Independent:
                current = CreateActive(definition, sourceId, nowMilliseconds);
                effects.Add(current);
                status = EffectApplyStatus.Applied;
                break;

            case EffectStackPolicy.Replace when existing.Length > 0:
                effects.RemoveAll(effect => effect.DefinitionId == effectId);
                current = CreateActive(definition, sourceId, nowMilliseconds);
                effects.Add(current);
                status = EffectApplyStatus.Replaced;
                break;

            case EffectStackPolicy.AddStack when existing.Length > 0:
                current = existing[0];
                current.Stacks = Math.Min(lifecycle.MaxStacks, current.Stacks + 1);
                RefreshTiming(current, definition, nowMilliseconds);
                status = EffectApplyStatus.Stacked;
                break;

            case EffectStackPolicy.RefreshDuration when existing.Length > 0:
                current = existing[0];
                RefreshTiming(current, definition, nowMilliseconds);
                status = EffectApplyStatus.Refreshed;
                break;

            default:
                current = CreateActive(definition, sourceId, nowMilliseconds);
                effects.Add(current);
                status = EffectApplyStatus.Applied;
                break;
        }

        SynchronizeProjection(target, nowMilliseconds);
        return new(status, effectId, current.Stacks, definition.OnApply);
    }

    /// <summary>
    /// Avanza ticks y expiraciones. Devuelve acciones a ejecutar por el orquestador de técnicas/combate.
    /// </summary>
    public IReadOnlyList<EffectPulse> Advance(LivingEntity target, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (!active.TryGetValue(target.Id, out var effects) || effects.Count == 0) return [];

        var pulses = new List<EffectPulse>();
        foreach (var runtime in effects.ToArray())
        {
            var definition = definitions.Get<EffectDefinition>(runtime.DefinitionId);
            var lifecycle = definition.Lifecycle;
            var expiresAt = runtime.ExpiresAtMilliseconds;
            var tickLimit = expiresAt > 0 ? Math.Min(nowMilliseconds, expiresAt) : nowMilliseconds;

            if (lifecycle.TickIntervalMilliseconds > 0 && runtime.NextTickAtMilliseconds > 0)
            {
                while (runtime.NextTickAtMilliseconds <= tickLimit)
                {
                    if (definition.OnTick.Length > 0)
                        pulses.Add(new(runtime.DefinitionId, runtime.SourceId, runtime.Stacks,
                            TechniqueActionMoment.Tick, definition.OnTick));
                    runtime.NextTickAtMilliseconds = checked(runtime.NextTickAtMilliseconds + lifecycle.TickIntervalMilliseconds);
                }
            }

            if (expiresAt > 0 && nowMilliseconds >= expiresAt)
            {
                if (definition.OnExpire.Length > 0)
                    pulses.Add(new(runtime.DefinitionId, runtime.SourceId, runtime.Stacks,
                        TechniqueActionMoment.Expire, definition.OnExpire));
                effects.Remove(runtime);
            }
        }

        if (effects.Count == 0) active.Remove(target.Id);
        SynchronizeProjection(target, nowMilliseconds);
        return pulses;
    }

    public bool Remove(LivingEntity target, DefinitionId effectId, bool requireDispellable = false, long nowMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (effectId.IsEmpty) return false;
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (!active.TryGetValue(target.Id, out var effects)) return false;

        if (requireDispellable && !definitions.Get<EffectDefinition>(effectId).Lifecycle.Dispellable) return false;
        var removed = effects.RemoveAll(effect => effect.DefinitionId == effectId) > 0;
        if (effects.Count == 0) active.Remove(target.Id);
        if (removed) SynchronizeProjection(target, nowMilliseconds);
        return removed;
    }

    public void Clear(LivingEntity target)
    {
        ArgumentNullException.ThrowIfNull(target);
        active.Remove(target.Id);
        target.Effects.Clear();
    }

    public float FlatStatModifier(LivingEntity target, StatId stat)
        => Sum(target, definition => definition.FlatStats.GetValueOrDefault(stat));

    public float PercentStatModifier(LivingEntity target, StatId stat)
        => Sum(target, definition => definition.PercentStats.GetValueOrDefault(stat));

    public float Modifier(LivingEntity target, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Modifier key vacío.", nameof(key));
        return Sum(target, definition => definition.Modifiers.GetValueOrDefault(key));
    }

    private float Sum(LivingEntity target, Func<EffectDefinition, float> selector)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!active.TryGetValue(target.Id, out var effects)) return 0;
        var total = 0f;
        foreach (var runtime in effects)
            total += selector(definitions.Get<EffectDefinition>(runtime.DefinitionId)) * runtime.Stacks;
        return total;
    }

    private ActiveEffect CreateActive(EffectDefinition definition, EntityId? sourceId, long nowMilliseconds)
    {
        var runtime = new ActiveEffect
        {
            InstanceId = Interlocked.Increment(ref nextInstanceId),
            DefinitionId = definition.Id,
            SourceId = sourceId,
            AppliedAtMilliseconds = nowMilliseconds
        };
        RefreshTiming(runtime, definition, nowMilliseconds);
        return runtime;
    }

    private static void RefreshTiming(ActiveEffect runtime, EffectDefinition definition, long nowMilliseconds)
    {
        var lifecycle = definition.Lifecycle;
        runtime.ExpiresAtMilliseconds = lifecycle.DurationMilliseconds > 0
            ? checked(nowMilliseconds + lifecycle.DurationMilliseconds)
            : 0;
        runtime.NextTickAtMilliseconds = lifecycle.TickIntervalMilliseconds > 0
            ? checked(nowMilliseconds + lifecycle.TickIntervalMilliseconds)
            : 0;
    }

    private void SynchronizeProjection(LivingEntity target, long nowMilliseconds)
    {
        target.Effects.Clear();
        if (!active.TryGetValue(target.Id, out var effects)) return;

        foreach (var runtime in effects.OrderBy(effect => effect.InstanceId))
        {
            var remaining = runtime.ExpiresAtMilliseconds <= 0
                ? TimeSpan.Zero
                : TimeSpan.FromMilliseconds(Math.Max(0, runtime.ExpiresAtMilliseconds - nowMilliseconds));
            target.Effects.Add(new EffectInstance(runtime.DefinitionId, remaining));
        }
    }
}
