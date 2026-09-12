using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;
using NuevoMMO.Server.World;

internal static class CombatLifecycleVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyTechniqueKillRewardsLootAndMobRespawn();
        VerifyPlayerDeathRespawnsAndMarksPersistenceDirty();
    }

    private static void VerifyTechniqueKillRewardsLootAndMobRespawn()
    {
        var registry = new DefinitionRegistry();
        var mapDefinition = CreateMap();
        var itemId = new DefinitionId(Guid.NewGuid());
        var lootId = new DefinitionId(Guid.NewGuid());
        var mobId = new DefinitionId(Guid.NewGuid());

        var item = new ItemDefinition(
            itemId,
            new ContentKey($"items.test.combat.{Guid.NewGuid():N}"),
            "Combat drop",
            null,
            true,
            1,
            ["test"],
            new ContentKey("visual.item.combat_drop"),
            groundDespawnMilliseconds: 1_000);
        var loot = new LootTableDefinition(
            lootId,
            new ContentKey($"loot.test.combat.{Guid.NewGuid():N}"),
            "Combat loot",
            null,
            true,
            1,
            ["test"],
            [itemId]);
        var mobDefinition = new MobDefinition(
            mobId,
            new ContentKey($"mobs.test.reward.{Guid.NewGuid():N}"),
            "Reward mob",
            null,
            true,
            1,
            ["test"],
            new ContentKey("visual.mob.reward"),
            lootTableId: lootId,
            behavior: new CreatureBehaviorDefinition(movement: CreatureMovementMode.Stationary),
            combat: new CreatureCombatDefinition(
                experience: 100,
                maxVitals: new Dictionary<VitalId, float> { [VitalId.Health] = 20 },
                parameters: new Dictionary<string, float> { ["respawnMilliseconds"] = 5 }));

        registry.Register(mapDefinition);
        registry.Register(item);
        registry.Register(loot);
        registry.Register(mobDefinition);

        var systems = new GameSystems(registry);
        var map = new MapInstance(new MapInstanceId(1), mapDefinition, 128);
        var player = CreatePlayer(new EntityId(1), map.Id, new Vector2Data(32, 32));
        var mob = new Mob(new EntityId(2), mobDefinition, map.Id, new Vector2Data(48, 32));
        map.Add(player);
        map.Add(mob);

        DefeatResolution? defeat = null;
        systems.DefeatResolved += value => defeat = value;
        var action = new TechniqueActionDefinition(
            TechniqueActionKind.Damage,
            TechniqueActionMoment.Impact,
            amount: 50,
            element: Element.Neutral);

        systems.Techniques.ExecuteEffectActions(player, mob, [action], 100);

        Check(!mob.IsAlive, "Combat lifecycle: técnica derrota al mob");
        Check(defeat is not null && defeat.RewardPlayer == player && defeat.ExperienceGranted == 100,
            "Combat lifecycle: resolución única entrega XP al atacante");
        Check(player.Experience == 100 && player.DirtyState,
            "Combat lifecycle: XP deja progreso marcado para autosave");

        systems.Advance(map, 101, 1);
        var groundDrops = map.Entities.All.OfType<WorldItem>().ToArray();
        Check(groundDrops.Length == 1 && groundDrops[0].DefinitionId == itemId,
            "Combat lifecycle: loot se materializa como WorldItem");

        systems.Advance(map, 105, 4);
        Check(mob.IsAlive && mob.Health == mob.MaxHealth && mob.Position == mob.SpawnPosition,
            "Combat lifecycle: mob reaparece en su spawn con vitales completos");
    }

    private static void VerifyPlayerDeathRespawnsAndMarksPersistenceDirty()
    {
        var registry = new DefinitionRegistry();
        var mapDefinition = CreateMap();
        var attackerDefinition = new MobDefinition(
            new DefinitionId(Guid.NewGuid()),
            new ContentKey($"mobs.test.player_death.{Guid.NewGuid():N}"),
            "Player death attacker",
            null,
            true,
            1,
            ["test"],
            new ContentKey("visual.mob.player_death"),
            behavior: new CreatureBehaviorDefinition(movement: CreatureMovementMode.Stationary),
            combat: new CreatureCombatDefinition(maxVitals: new Dictionary<VitalId, float> { [VitalId.Health] = 100 }));
        registry.Register(mapDefinition);
        registry.Register(attackerDefinition);

        var systems = new GameSystems(registry);
        var map = new MapInstance(new MapInstanceId(2), mapDefinition, 128);
        var player = CreatePlayer(new EntityId(10), map.Id, new Vector2Data(96, 96));
        var attacker = new Mob(new EntityId(11), attackerDefinition, map.Id, new Vector2Data(80, 96));
        map.Add(player);
        map.Add(attacker);
        player.MarkSaved();

        systems.Combat.ApplyDamage(attacker, player, 1_000_000, Element.Neutral, nowMilliseconds: 1_000);
        Check(!player.IsAlive && player.DirtyState,
            "Combat lifecycle: muerte de jugador se marca para persistencia");

        systems.Advance(map, 1_001, 1);
        systems.Advance(map, 4_000, 2_999);

        Check(player.IsAlive && player.Health == Math.Max(1, player.MaxHealth / 2),
            "Combat lifecycle: jugador reaparece con recuperación parcial");
        Check(player.Position == mapDefinition.Spawn && player.DirtyState,
            "Combat lifecycle: respawn usa spawn del mapa y queda pendiente de autosave");
    }

    private static MapDefinition CreateMap()
        => new(
            new DefinitionId(Guid.NewGuid()),
            new ContentKey($"maps.test.combat_lifecycle.{Guid.NewGuid():N}"),
            "Combat lifecycle map",
            null,
            true,
            1,
            ["test"],
            new ContentKey("visual.map.combat_lifecycle"),
            new BoundsData(new Vector2Data(0, 0), new Vector2Data(512, 512)),
            new Vector2Data(16, 16),
            new Vector2IntData(32, 32));

    private static Player CreatePlayer(EntityId id, MapInstanceId map, Vector2Data position)
        => new(
            id,
            new AccountId(Guid.NewGuid()),
            new CharacterId(Guid.NewGuid()),
            map,
            position,
            new ContentKey("visual.player.combat_lifecycle"),
            "Combat lifecycle tester");

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
