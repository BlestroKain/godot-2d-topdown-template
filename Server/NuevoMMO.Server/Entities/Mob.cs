using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class Mob : LivingEntity
{
    private readonly Dictionary<EntityId, float> threat = [];

    public Mob(EntityId id, DefinitionId definition, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (definition.IsEmpty) throw new ArgumentException("DefinitionId inválido.", nameof(definition));
        DefinitionId = definition;
        SpawnPosition = position;
        Behavior = new CreatureBehaviorDefinition();
        Combat = new CreatureCombatDefinition();
    }

    public Mob(EntityId id, MobDefinition definition, MapInstanceId mapInstance, Vector2Data position)
        : base(id, mapInstance, position, RequireDefinition(definition).VisualKey, definition.Name,
            ReadMaxVital(definition.Combat, VitalId.Health, 100, 1),
            ReadMaxVital(definition.Combat, VitalId.Mana, 100, 0))
    {
        DefinitionId = definition.Id;
        SpawnPosition = position;
        Behavior = definition.Behavior;
        Combat = definition.Combat;
        if (definition.Collision is { } collision) ConfigureCollision(collision.ToRuntime());
        ApplyCombatProfile(definition.Combat);
    }

    public DefinitionId DefinitionId { get; }
    public CreatureBehaviorDefinition Behavior { get; }
    public CreatureCombatDefinition Combat { get; }
    public int Level => Combat.Level;
    public long ExperienceReward => Combat.Experience;
    public Vector2Data SpawnPosition { get; }
    public Vector2Data? AggroOrigin { get; private set; }
    public long LastBasicAttackMilliseconds { get; private set; }
    public long LastTechniqueMilliseconds { get; private set; }
    public IReadOnlyDictionary<EntityId, float> Threat => threat;

    public void BeginAggro(EntityId target, Vector2Data? origin = null)
    {
        if (target.Value <= 0) throw new ArgumentException("Target inválido.", nameof(target));
        AggroOrigin ??= origin ?? Position;
        EnterCombat(target);
    }
    public void ResetAggro(bool clearThreat = true) { AggroOrigin = null; LeaveCombat(); if (clearThreat) threat.Clear(); }
    public void AddThreat(EntityId source, float amount)
    {
        if (source.Value <= 0) throw new ArgumentException("Source inválido.", nameof(source));
        if (!float.IsFinite(amount) || amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        threat[source] = threat.GetValueOrDefault(source) + amount;
    }
    public bool RemoveThreat(EntityId source) => threat.Remove(source);
    public EntityId? HighestThreat(Func<EntityId, bool>? predicate = null)
    {
        var candidates = predicate is null ? threat : threat.Where(pair => predicate(pair.Key)).ToDictionary();
        return candidates.Count == 0 ? null : candidates.MaxBy(static pair => pair.Value).Key;
    }
    public void ClearThreat() => threat.Clear();
    public bool IsOutsideResetRadius()
    {
        if (Behavior.ResetRadius <= 0 || AggroOrigin is not { } origin) return false;
        var delta = Position - origin; return delta.LengthSquared > Behavior.ResetRadius * Behavior.ResetRadius;
    }
    public bool ShouldFlee() => IsAlive && Behavior.FleeHealthPercentage > 0 && HealthPercent <= Behavior.FleeHealthPercentage;
    public bool CanBasicAttack(long nowMilliseconds) => IsAlive && nowMilliseconds >= LastBasicAttackMilliseconds + Math.Max(0, Combat.AttackIntervalMilliseconds);
    public void MarkBasicAttack(long nowMilliseconds) { if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds)); LastBasicAttackMilliseconds = nowMilliseconds; }
    public bool CanUseTechnique(long nowMilliseconds) => IsAlive && nowMilliseconds >= LastTechniqueMilliseconds + Math.Max(0, Combat.TechniqueIntervalMilliseconds);
    public void MarkTechnique(long nowMilliseconds) { if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds)); LastTechniqueMilliseconds = nowMilliseconds; }

    public override EntityState ToState() => new MobState(Id, DefinitionId, MapInstanceId, Position, Velocity, Direction, VisualKey, DisplayName);
    private static MobDefinition RequireDefinition(MobDefinition? definition) => definition ?? throw new ArgumentNullException(nameof(definition));
    private static int ReadMaxVital(CreatureCombatDefinition combat, VitalId vital, int fallback, int minimum)
    {
        if (!combat.MaxVitals.TryGetValue(vital, out var value)) return fallback;
        if (!float.IsFinite(value) || value < minimum || value > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(combat), $"Vital {vital} inválido.");
        return Math.Max(minimum, (int)MathF.Round(value, MidpointRounding.AwayFromZero));
    }
}
