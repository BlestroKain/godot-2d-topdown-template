using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class Npc : LivingEntity
{
    public Npc(EntityId id, DefinitionId definition, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (definition.IsEmpty) throw new ArgumentException("DefinitionId inválido.", nameof(definition));
        DefinitionId = definition;
        Behavior = new CreatureBehaviorDefinition();
    }

    public Npc(EntityId id, NpcDefinition definition, MapInstanceId mapInstance, Vector2Data position)
        : base(id, mapInstance, position, RequireDefinition(definition).VisualKey, definition.Name,
            definition.Combat is { } combat ? ReadMaxVital(combat, VitalId.Health, 100, 1) : 100,
            definition.Combat is { } manaProfile ? ReadMaxVital(manaProfile, VitalId.Mana, 100, 0) : 100)
    {
        DefinitionId = definition.Id;
        CombatEnabled = definition.CombatEnabled;
        Behavior = definition.Behavior;
        Combat = definition.Combat;
        SpawnPosition = position;
        if (definition.Collision is { } collision) ConfigureCollision(collision.ToRuntime());
        if (Combat is not null) ApplyCombatProfile(Combat);
    }

    public DefinitionId DefinitionId { get; }
    public bool CombatEnabled { get; }
    public CreatureBehaviorDefinition Behavior { get; }
    public CreatureCombatDefinition? Combat { get; }
    public Vector2Data SpawnPosition { get; }
    public Vector2Data? AggroOrigin { get; private set; }
    public bool CanParticipateInCombat => CombatEnabled && Combat is not null && IsAlive;
    public void BeginAggro(EntityId target)
    {
        if (!CanParticipateInCombat) throw new InvalidOperationException("Este NPC no participa en combate.");
        if (target.Value <= 0) throw new ArgumentException("Target inválido.", nameof(target));
        AggroOrigin ??= Position; EnterCombat(target);
    }
    public void ResetAggro() { AggroOrigin = null; LeaveCombat(); }
    public override EntityState ToState() => new NpcState(Id, DefinitionId, MapInstanceId, Position, Velocity, Direction, VisualKey, DisplayName);
    private static NpcDefinition RequireDefinition(NpcDefinition? definition) => definition ?? throw new ArgumentNullException(nameof(definition));
    private static int ReadMaxVital(CreatureCombatDefinition combat, VitalId vital, int fallback, int minimum)
    {
        if (!combat.MaxVitals.TryGetValue(vital, out var value)) return fallback;
        if (!float.IsFinite(value) || value < minimum || value > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(combat), $"Vital {vital} inválido.");
        return Math.Max(minimum, (int)MathF.Round(value, MidpointRounding.AwayFromZero));
    }
}
