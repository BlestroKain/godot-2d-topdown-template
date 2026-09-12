using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.Systems;

/// <summary>
/// Raíz de composición de sistemas de gameplay del servidor. Evita que WorldRuntime y los handlers
/// construyan dependencias ad-hoc y permite sustituir piezas en tests sin contaminar las Entities.
/// </summary>
public sealed class GameSystems
{
    private const int PlayerRespawnMilliseconds = 3_000;
    private const int DefaultMobRespawnMilliseconds = 10_000;
    private readonly float mobMovementSpeed;
    private readonly object lifecycleGate = new();
    private readonly Dictionary<MapInstanceId, Queue<DefeatResolution>> pendingDefeats = [];
    private readonly Dictionary<EntityId, long> respawnAt = [];
    private long nextTransientEntityId = long.MaxValue;

    public GameSystems(
        DefinitionRegistry definitions,
        ILootRandomSource? lootRandom = null,
        ITechniqueResourceAccess? techniqueResources = null,
        Func<Player, ConditionGroupDefinition, bool>? requirementsEvaluator = null,
        Func<Entity, Vector2Data, bool>? lineOfSight = null,
        LevelProgressionDefinition? levelProgression = null,
        float mobMovementSpeed = 0)
    {
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        if (!float.IsFinite(mobMovementSpeed) || mobMovementSpeed < 0)
            throw new ArgumentOutOfRangeException(nameof(mobMovementSpeed));
        this.mobMovementSpeed = mobMovementSpeed;

        Conditions = new ConditionSystem();
        Combat = new CombatSystem();
        Effects = new EffectSystem(definitions);
        Inventory = new InventorySystem(definitions);
        Equipment = new EquipmentSystem(definitions);
        Progression = new ProgressionSystem(levelProgression, Equipment);
        Loot = new LootSystem(definitions, lootRandom);
        Defeats = new DefeatSystem(definitions, Progression, Loot);
        Professions = new ProfessionSystem(definitions);
        Knowledge = new KnowledgeSystem();
        Projectiles = new ProjectileSystem();
        Harvesting = new ResourceHarvestSystem(definitions, Loot);
        Interactions = new InteractionSystem(Inventory, harvesting: Harvesting);
        Ai = new AiSystem();
        Techniques = new TechniqueSystem(
            definitions,
            Combat,
            Effects,
            techniqueResources,
            requirementsEvaluator ?? ((player, group) => Conditions.Evaluate(player, group)),
            lineOfSight);
        Events = new EventRuntime(definitions, Conditions, Progression, Inventory, Loot, Effects);
        Combat.EntityDefeated += ResolveDefeat;
    }

    public DefinitionRegistry Definitions { get; }
    public ConditionSystem Conditions { get; }
    public CombatSystem Combat { get; }
    public EffectSystem Effects { get; }
    public InventorySystem Inventory { get; }
    public EquipmentSystem Equipment { get; }
    public ProgressionSystem Progression { get; }
    public LootSystem Loot { get; }
    public DefeatSystem Defeats { get; }
    public ProfessionSystem Professions { get; }
    public KnowledgeSystem Knowledge { get; }
    public ProjectileSystem Projectiles { get; }
    public ResourceHarvestSystem Harvesting { get; }
    public InteractionSystem Interactions { get; }
    public AiSystem Ai { get; }
    public TechniqueSystem Techniques { get; }
    public EventRuntime Events { get; }
    public event Action<DefeatResolution>? DefeatResolved;

    /// <summary>
    /// Avanza el ciclo autoritativo de gameplay. Las derrotas producidas por handlers, casts,
    /// efectos, proyectiles o zonas se consumen aquí para que loot y respawn tengan un único reloj.
    /// </summary>
    public void Advance(MapInstance map, long nowMilliseconds, int deltaMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (deltaMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaMilliseconds));

        ProcessPendingDefeats(map);
        ProcessRespawns(map, nowMilliseconds);
        AdvanceMobAi(map, nowMilliseconds, deltaMilliseconds);

        var snapshot = map.Entities.All.ToArray();
        foreach (var living in snapshot.OfType<LivingEntity>())
        {
            var pulses = Effects.Advance(living, nowMilliseconds);
            foreach (var pulse in pulses)
            {
                LivingEntity? source = null;
                if (pulse.SourceId is { } sourceId && map.Entities.TryGet(sourceId, out var sourceEntity))
                    source = sourceEntity as LivingEntity;
                Techniques.ExecuteEffectActions(source, living, pulse.Actions, nowMilliseconds);
            }
        }

        Techniques.Advance(nowMilliseconds);
    }

    private void AdvanceMobAi(MapInstance map, long nowMilliseconds, int deltaMilliseconds)
    {
        var candidates = map.Entities.All.OfType<LivingEntity>().Where(static living => living.IsAlive).ToArray();
        foreach (var mob in candidates.OfType<Mob>())
        {
            var decision = Ai.Evaluate(mob, candidates, nowMilliseconds);
            switch (decision.Action)
            {
                case MobAiAction.Chase:
                case MobAiAction.Flee:
                case MobAiAction.ReturnToOrigin:
                    if (mobMovementSpeed > 0)
                        MobMovementSystem.ApplyDirection(mob, map.Definition, decision.DesiredDirection, mobMovementSpeed, deltaMilliseconds);
                    break;
                case MobAiAction.BasicAttack:
                    if (decision.Target is { } targetId &&
                        map.Entities.TryGet(targetId, out var entity) &&
                        entity is LivingEntity target && target.IsAlive)
                    {
                        Combat.ExecuteMobBasicAttack(mob, target, nowMilliseconds);
                    }
                    break;
                case MobAiAction.Idle:
                case MobAiAction.Wander:
                default:
                    break;
            }
        }
    }

    private void ResolveDefeat(CombatDefeatEvent defeat)
    {
        var resolution = Defeats.Resolve(defeat);
        lock (lifecycleGate)
        {
            if (!pendingDefeats.TryGetValue(defeat.Target.MapInstanceId, out var queue))
                pendingDefeats[defeat.Target.MapInstanceId] = queue = new Queue<DefeatResolution>();
            queue.Enqueue(resolution);
        }
        DefeatResolved?.Invoke(resolution);
    }

    private void ProcessPendingDefeats(MapInstance map)
    {
        DefeatResolution[] pending;
        lock (lifecycleGate)
        {
            if (!pendingDefeats.TryGetValue(map.Id, out var queue) || queue.Count == 0) return;
            pending = queue.ToArray();
            queue.Clear();
            pendingDefeats.Remove(map.Id);
        }

        foreach (var resolution in pending)
        {
            if (resolution.Defeated is Mob mob)
            {
                MaterializeLoot(map, mob.Position, resolution.Loot, resolution.OccurredAtMilliseconds);
                var delay = ResolveMobRespawnDelay(mob);
                if (delay > 0)
                    ScheduleRespawn(mob.Id, checked(resolution.OccurredAtMilliseconds + delay));
            }
            else if (resolution.Defeated is Player player)
            {
                ScheduleRespawn(player.Id, checked(resolution.OccurredAtMilliseconds + PlayerRespawnMilliseconds));
            }
        }
    }

    private void ProcessRespawns(MapInstance map, long nowMilliseconds)
    {
        foreach (var living in map.Entities.All.OfType<LivingEntity>().ToArray())
        {
            if (living.IsAlive || !TryGetRespawn(living.Id, out var due) || nowMilliseconds < due) continue;

            Effects.Clear(living);
            switch (living)
            {
                case Player player:
                    player.TransferTo(map.Id, map.Definition.Spawn);
                    player.Revive(Math.Max(1, player.MaxHealth / 2), Math.Max(0, player.MaxMana / 2));
                    player.MarkDirty();
                    break;
                case Mob mob:
                    mob.MoveTo(mob.SpawnPosition, Vector2Data.Zero);
                    mob.Revive(mob.MaxHealth, mob.MaxMana);
                    mob.ResetAggro();
                    break;
            }
            RemoveRespawn(living.Id);
        }
    }

    private void MaterializeLoot(MapInstance map, Vector2Data position, IReadOnlyList<LootRoll> rolls, long nowMilliseconds)
    {
        foreach (var roll in rolls)
        {
            if (!Definitions.TryGet<ItemDefinition>(roll.Item.DefinitionId, out var definition) || definition is null)
                continue;
            var despawnAt = definition.GroundDespawnMilliseconds > 0
                ? checked(nowMilliseconds + definition.GroundDespawnMilliseconds)
                : 0;
            var worldItem = new WorldItem(
                AllocateTransientEntityId(map),
                roll.Item,
                map.Id,
                map.Definition.Bounds.Clamp(position),
                definition.VisualKey,
                definition.Name,
                despawnAt);
            map.Add(worldItem);
        }
    }

    private int ResolveMobRespawnDelay(Mob mob)
    {
        if (!mob.Combat.Parameters.TryGetValue("respawnMilliseconds", out var configured))
            return DefaultMobRespawnMilliseconds;
        if (!float.IsFinite(configured) || configured <= 0) return 0;
        return configured >= int.MaxValue ? int.MaxValue : (int)MathF.Round(configured, MidpointRounding.AwayFromZero);
    }

    private EntityId AllocateTransientEntityId(MapInstance map)
    {
        while (true)
        {
            var value = Interlocked.Decrement(ref nextTransientEntityId);
            if (value <= 0) throw new InvalidOperationException("Se agotó el espacio de EntityId transitorio.");
            var id = new EntityId(value);
            if (!map.Entities.TryGet(id, out _)) return id;
        }
    }

    private void ScheduleRespawn(EntityId entity, long due)
    {
        lock (lifecycleGate) respawnAt[entity] = due;
    }

    private bool TryGetRespawn(EntityId entity, out long due)
    {
        lock (lifecycleGate) return respawnAt.TryGetValue(entity, out due);
    }

    private void RemoveRespawn(EntityId entity)
    {
        lock (lifecycleGate) respawnAt.Remove(entity);
    }

    public void OnEntityRemoved(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        RemoveRespawn(entity.Id);
        if (entity is LivingEntity living) Effects.Clear(living);
        if (entity is Player player) Techniques.Cancel(player.Id);
    }
}
