using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public enum MobAiAction : byte
{
    Idle,
    Wander,
    Chase,
    Flee,
    ReturnToOrigin,
    BasicAttack
}

public sealed record MobAiDecision(
    MobAiAction Action,
    EntityId? Target = null,
    Vector2Data DesiredDirection = default,
    float DistanceToTarget = 0);

/// <summary>
/// Toma decisiones de alto nivel para Mobs usando CreatureBehaviorDefinition.
/// No mueve entidades ni envía packets: WorldRuntime decide cómo ejecutar la intención.
/// </summary>
public sealed class AiSystem
{
    public MobAiDecision Evaluate(Mob mob, IEnumerable<LivingEntity> candidates, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(mob);
        ArgumentNullException.ThrowIfNull(candidates);
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (!mob.IsAlive) return new MobAiDecision(MobAiAction.Idle);

        var available = candidates
            .Where(candidate => CanTarget(mob, candidate))
            .Select(candidate => new TargetCandidate(candidate, mob.Position.DistanceTo(candidate.Position)))
            .Where(candidate => mob.Behavior.SightRange <= 0 || candidate.Distance <= mob.Behavior.SightRange)
            .ToArray();

        if (mob.IsOutsideResetRadius())
        {
            var origin = mob.AggroOrigin ?? mob.SpawnPosition;
            var delta = origin - mob.Position;
            if (delta.Length <= 1f)
            {
                mob.ResetAggro();
                return DefaultIdleAction(mob);
            }
            return new MobAiDecision(MobAiAction.ReturnToOrigin, null, delta.Normalized(), delta.Length);
        }

        var target = ResolveCurrentOrNextTarget(mob, available);
        if (target is null)
        {
            if (mob.CombatState.InCombat) mob.ResetAggro();
            return DefaultIdleAction(mob);
        }

        if (mob.CombatState.Target != target.Entity.Id)
            mob.BeginAggro(target.Entity.Id);

        var directionToTarget = (target.Entity.Position - mob.Position).Normalized();
        if (mob.ShouldFlee())
            return new MobAiDecision(MobAiAction.Flee, target.Entity.Id, -directionToTarget, target.Distance);

        var attackRange = BasicAttackRange(mob);
        if (attackRange > 0 && target.Distance <= attackRange && mob.CanBasicAttack(nowMilliseconds))
            return new MobAiDecision(MobAiAction.BasicAttack, target.Entity.Id, directionToTarget, target.Distance);

        return new MobAiDecision(MobAiAction.Chase, target.Entity.Id, directionToTarget, target.Distance);
    }

    public LivingEntity? ResolveTarget(Mob mob, IEnumerable<LivingEntity> candidates)
    {
        ArgumentNullException.ThrowIfNull(mob);
        ArgumentNullException.ThrowIfNull(candidates);
        var available = candidates
            .Where(candidate => CanTarget(mob, candidate))
            .Select(candidate => new TargetCandidate(candidate, mob.Position.DistanceTo(candidate.Position)))
            .Where(candidate => mob.Behavior.SightRange <= 0 || candidate.Distance <= mob.Behavior.SightRange)
            .ToArray();
        return ResolveCurrentOrNextTarget(mob, available)?.Entity;
    }

    public bool CanTarget(Mob mob, LivingEntity candidate)
    {
        ArgumentNullException.ThrowIfNull(mob);
        ArgumentNullException.ThrowIfNull(candidate);
        if (!candidate.IsAlive || candidate.Id == mob.Id || candidate.MapInstanceId != mob.MapInstanceId) return false;

        if (candidate is Player) return mob.Behavior.Aggressive || mob.Threat.ContainsKey(candidate.Id);

        if (!mob.Behavior.NpcVsNpcEnabled && !mob.Threat.ContainsKey(candidate.Id)) return false;
        if (candidate is Mob otherMob && otherMob.DefinitionId == mob.DefinitionId && !mob.Behavior.AttackAllies) return false;
        if (candidate is Npc npc && !npc.CanParticipateInCombat) return false;
        return true;
    }

    private static MobAiDecision DefaultIdleAction(Mob mob)
        => mob.Behavior.Movement == CreatureMovementMode.Stationary
            ? new MobAiDecision(MobAiAction.Idle)
            : new MobAiDecision(MobAiAction.Wander);

    private static TargetCandidate? ResolveCurrentOrNextTarget(Mob mob, IReadOnlyList<TargetCandidate> available)
    {
        if (mob.CombatState.Target is { } current)
        {
            var existing = available.FirstOrDefault(candidate => candidate.Entity.Id == current);
            if (existing is not null) return existing;
        }

        var threatened = available.Where(candidate => mob.Threat.ContainsKey(candidate.Entity.Id)).ToArray();
        if (threatened.Length > 0)
        {
            var highestThreat = mob.HighestThreat(id => threatened.Any(candidate => candidate.Entity.Id == id));
            if (highestThreat is { } threatTarget)
                return threatened.First(candidate => candidate.Entity.Id == threatTarget);
        }

        if (!mob.Behavior.Aggressive || available.Count == 0) return null;
        return mob.Behavior.TargetPriority switch
        {
            CreatureTargetPriority.Nearest => available.MinBy(static candidate => candidate.Distance),
            CreatureTargetPriority.HighestDamage => available.MaxBy(candidate => mob.Threat.GetValueOrDefault(candidate.Entity.Id)),
            CreatureTargetPriority.LowestHealth => available.MinBy(static candidate => candidate.Entity.HealthPercent),
            CreatureTargetPriority.Random => available[Random.Shared.Next(available.Count)],
            _ => available[0]
        };
    }

    private static float BasicAttackRange(Mob mob)
    {
        if (!mob.Combat.Parameters.TryGetValue("basicAttackRange", out var range)) return 0;
        return float.IsFinite(range) && range > 0 ? range : 0;
    }

    private sealed record TargetCandidate(LivingEntity Entity, float Distance);
}
