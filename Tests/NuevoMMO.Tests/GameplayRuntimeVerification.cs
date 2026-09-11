using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;
using NuevoMMO.Server.World;

internal static class GameplayRuntimeVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyMobAiUsesAuthoritativeMovementAndCombat();
        VerifyProjectilePayloadUsesTechniquePipeline();
        VerifyEventRuntimeUsesProgressionSystem();
    }

    private static void VerifyMobAiUsesAuthoritativeMovementAndCombat()
    {
        var registry = new DefinitionRegistry();
        var mapDefinition = CreateMap();
        var mobDefinition = CreateAggressiveMob();
        registry.Register(mapDefinition);
        registry.Register(mobDefinition);

        var systems = new GameSystems(registry, mobMovementSpeed: 100);
        var map = new MapInstance(new MapInstanceId(1), mapDefinition, 128);
        var player = CreatePlayer(new EntityId(1), new MapInstanceId(1), new Vector2Data(120, 20));
        var mob = new Mob(new EntityId(2), mobDefinition, new MapInstanceId(1), new Vector2Data(20, 20));
        map.Add(player);
        map.Add(mob);

        systems.Advance(map, 100, 100);
        Check(mob.Position.X > 20 && mob.Position.X < player.Position.X && mob.CombatState.Target == player.Id,
            "AI: mob agresivo persigue con movimiento autoritativo");

        mob.MoveTo(new Vector2Data(110, 20), Vector2Data.Zero);
        var healthBefore = player.Health;
        systems.Advance(map, 200, 100);
        Check(player.Health < healthBefore && mob.LastBasicAttackMilliseconds == 200,
            "AI: mob en alcance ejecuta ataque básico por CombatSystem");
    }

    private static void VerifyProjectilePayloadUsesTechniquePipeline()
    {
        var registry = new DefinitionRegistry();
        var mapDefinition = CreateMap();
        var targetDefinition = CreatePassiveMob();
        registry.Register(mapDefinition);
        registry.Register(targetDefinition);
        var systems = new GameSystems(registry);

        var source = CreatePlayer(new EntityId(10), new MapInstanceId(2), new Vector2Data(20, 20));
        var target = new Mob(new EntityId(11), targetDefinition, new MapInstanceId(2), new Vector2Data(40, 20));
        var damage = new TechniqueActionDefinition(
            TechniqueActionKind.Damage,
            TechniqueActionMoment.Impact,
            amount: 25,
            element: Element.Fire);
        var projectile = new Projectile(
            new EntityId(12),
            null,
            source.Id,
            null,
            new MapInstanceId(2),
            target.Position,
            Vector2Data.Right,
            100,
            100,
            1000,
            1,
            new ContentKey("visual.projectile.test"),
            "Projectile test",
            impactActions: [damage]);

        var healthBefore = target.Health;
        Check(systems.Projectiles.TryRegisterImpact(projectile, target, 16),
            "Projectile: registra impacto válido");
        systems.Techniques.ExecuteEffectActions(source, target, projectile.ImpactActions, 1000);
        Check(target.Health == healthBefore - 25,
            "Projectile: payload de impacto usa TechniqueSystem sin daño hardcodeado");
    }

    private static void VerifyEventRuntimeUsesProgressionSystem()
    {
        var registry = new DefinitionRegistry();
        var eventId = new DefinitionId(Guid.NewGuid());
        var listId = Guid.NewGuid();
        var page = new EventPageDefinition(
            Guid.NewGuid(),
            EventTrigger.Action,
            commandLists: new Dictionary<Guid, EventCommandDefinition[]>
            {
                [listId] =
                [
                    new EventCommandDefinition(
                        Guid.NewGuid(),
                        EventCommandKind.GiveExperience,
                        numbers: new Dictionary<string, float> { ["amount"] = 100 })
                ]
            },
            rootCommandListId: listId);
        var definition = new EventDefinition(
            eventId,
            new ContentKey("events.test.progression"),
            "Progression event",
            null,
            true,
            1,
            ["test"],
            EventScope.Common,
            pages: [page]);
        registry.Register(definition);

        var systems = new GameSystems(registry);
        var player = CreatePlayer(new EntityId(20), new MapInstanceId(3), new Vector2Data(10, 10));
        var result = systems.Events.TryTrigger(player, definition, EventTrigger.Action, 100);

        Check(result.Success && player.Level == 2 && player.AttributePoints == 3,
            "Events: GiveExperience usa ProgressionSystem real");
    }

    private static MapDefinition CreateMap()
        => new(
            new DefinitionId(Guid.NewGuid()),
            new ContentKey($"maps.test.{Guid.NewGuid():N}"),
            "Test map",
            null,
            true,
            1,
            ["test"],
            new ContentKey("visual.map.test"),
            new BoundsData(new Vector2Data(0, 0), new Vector2Data(512, 512)),
            new Vector2Data(16, 16),
            new Vector2IntData(32, 32));

    private static MobDefinition CreateAggressiveMob()
        => new(
            new DefinitionId(Guid.NewGuid()),
            new ContentKey($"mobs.test.aggressive.{Guid.NewGuid():N}"),
            "Aggressive test mob",
            null,
            true,
            1,
            ["test"],
            new ContentKey("visual.mob.test"),
            behavior: new CreatureBehaviorDefinition(
                aggressive: true,
                movement: CreatureMovementMode.Wander,
                sightRange: 500,
                resetRadius: 500),
            combat: new CreatureCombatDefinition(
                baseDamage: 20,
                attackIntervalMilliseconds: 0,
                maxVitals: new Dictionary<VitalId, float> { [VitalId.Health] = 100 },
                parameters: new Dictionary<string, float> { ["basicAttackRange"] = 16 }));

    private static MobDefinition CreatePassiveMob()
        => new(
            new DefinitionId(Guid.NewGuid()),
            new ContentKey($"mobs.test.passive.{Guid.NewGuid():N}"),
            "Passive test mob",
            null,
            true,
            1,
            ["test"],
            new ContentKey("visual.mob.test"),
            behavior: new CreatureBehaviorDefinition(movement: CreatureMovementMode.Stationary),
            combat: new CreatureCombatDefinition(
                maxVitals: new Dictionary<VitalId, float> { [VitalId.Health] = 100 }));

    private static Player CreatePlayer(EntityId id, MapInstanceId map, Vector2Data position)
        => new(
            id,
            new AccountId(Guid.NewGuid()),
            new CharacterId(Guid.NewGuid()),
            map,
            position,
            new ContentKey("visual.player.test"),
            "Runtime tester");

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
