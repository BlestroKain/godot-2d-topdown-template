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
    public GameSystems(
        DefinitionRegistry definitions,
        ILootRandomSource? lootRandom = null,
        ITechniqueResourceAccess? techniqueResources = null,
        Func<Player, ConditionGroupDefinition, bool>? requirementsEvaluator = null,
        Func<Entity, Vector2Data, bool>? lineOfSight = null,
        LevelProgressionDefinition? levelProgression = null)
    {
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        Conditions = new ConditionSystem();
        Combat = new CombatSystem();
        Effects = new EffectSystem(definitions);
        Inventory = new InventorySystem(definitions);
        Equipment = new EquipmentSystem(definitions);
        Progression = new ProgressionSystem(levelProgression, Equipment);
        Loot = new LootSystem(definitions, lootRandom);
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
    }

    public DefinitionRegistry Definitions { get; }
    public ConditionSystem Conditions { get; }
    public CombatSystem Combat { get; }
    public EffectSystem Effects { get; }
    public InventorySystem Inventory { get; }
    public EquipmentSystem Equipment { get; }
    public ProgressionSystem Progression { get; }
    public LootSystem Loot { get; }
    public ProfessionSystem Professions { get; }
    public KnowledgeSystem Knowledge { get; }
    public ProjectileSystem Projectiles { get; }
    public ResourceHarvestSystem Harvesting { get; }
    public InteractionSystem Interactions { get; }
    public AiSystem Ai { get; }
    public TechniqueSystem Techniques { get; }

    /// <summary>
    /// Avanza efectos y casts/channels. Movimiento, proyectiles y respawn de recursos continúan
    /// siendo recorridos por WorldRuntime para conservar un único orden de actualización del mapa.
    /// </summary>
    public void Advance(MapInstance map, long nowMilliseconds, int deltaMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (deltaMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaMilliseconds));

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

    public void OnEntityRemoved(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is LivingEntity living) Effects.Clear(living);
        if (entity is Player player) Techniques.Cancel(player.Id);
    }
}
