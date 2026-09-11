using System.Runtime.CompilerServices;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.NetworkHandlers;
using NuevoMMO.Server.Security;
using NuevoMMO.Server.Systems;
using NuevoMMO.Server.World;

internal static class ProgressionNetworkVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        RoundtripPackets();
        VerifyDevelopmentWorldAndXpLoop();
    }

    private static void RoundtripPackets()
    {
        var allocate = new AllocateAttributeRequest(PrimaryAttributeId.Vitality, 2);
        Check(PacketCodec.Decode(PacketCodec.Encode(allocate)) == allocate,
            "Network progression: AllocateAttribute roundtrip");

        var stats = new PlayerStatsPacket(new PlayerStatsSnapshot(
            2, 25, 300, 2,
            new(10, 12, 1), new(10, 10, 1), new(10, 10, 1), new(10, 10, 1), new(11, 13, 1),
            440, 452, 144, 164, 3.25f, 1.2f, 3.6f, 4,
            5, 6, 7, 8, 9));
        Check(PacketCodec.Decode(PacketCodec.Encode(stats)) == stats,
            "Network progression: PlayerStats roundtrip");

        var attack = new DevelopmentAttackRequest(new EntityId(77), DevelopmentAttackKind.Fire);
        Check(PacketCodec.Decode(PacketCodec.Encode(attack)) == attack,
            "Network combat: DevelopmentAttack roundtrip");

        var combat = new CombatDebugPacket(
            new EntityId(77), DevelopmentAttackKind.Fire, Element.Fire, PrimaryAttributeId.Intelligence,
            110, 20, 88, false, 412, 500, 17.6f, 8.8f, 88, 1, 0);
        Check(PacketCodec.Decode(PacketCodec.Encode(combat)) == combat,
            "Network combat: CombatDebug roundtrip");
    }

    private static void VerifyDevelopmentWorldAndXpLoop()
    {
        var composition = DevelopmentWorldFactory.Create("Development");
        var mobs = composition.World.MapInstance.Entities.All.OfType<Mob>().ToArray();
        var dummy = mobs.Single(mob => mob.DefinitionId == TrainingDummyFixture.DefinitionId);
        var xpMob = mobs.Single(mob => mob.DefinitionId != TrainingDummyFixture.DefinitionId);

        Check(dummy.Immortal && dummy.MaxHealth == 50_000 && dummy.ExperienceReward == 0,
            "Development: dummy real visible 50k/inmortal/0XP");
        Check(xpMob.MaxHealth == 250 && xpMob.ExperienceReward == 100,
            "Development: mob XP técnico 250HP/100XP");

        var account = new AccountId(Guid.NewGuid());
        var repository = new InMemoryCharacterRepository();
        var record = repository.CreateAsync(account, "NetworkProgressionTester", composition.World.Map.Id, composition.World.Map.Spawn)
            .GetAwaiter().GetResult();
        var persistence = new PersistenceService(repository, composition.World.Map);
        var player = new Player(
            new EntityId(700), account, record.Id, composition.World.Instance, composition.World.Map.Spawn,
            new ContentKey("template.player"), record.Name);
        composition.Systems.Progression.Initialize(player, ProgressionRules.CreateInitial(), preserveVitals: false);

        var session = new PlayerSession(new ConnectionId(Guid.NewGuid()))
        {
            State = PlayerSessionState.InWorld,
            Account = account,
            Character = record.Id,
            Player = player
        };
        var sent = new List<IPacket>();
        var context = new ServerPacketContext
        {
            Connection = session.Connection,
            Session = session,
            Send = sent.Add
        };
        var handler = new DevelopmentAttackHandler(
            composition.World,
            composition.Systems.Combat,
            composition.Systems.Progression,
            persistence,
            new AuthorizationService());

        for (var hit = 0; hit < 3; hit++)
            handler.HandleAsync(context, new DevelopmentAttackRequest(xpMob.Id, DevelopmentAttackKind.NeutralStrength), CancellationToken.None)
                .AsTask().GetAwaiter().GetResult();

        Check(!xpMob.IsAlive && player.Level == 2 && player.AttributePoints == 3,
            "Combat/XP: matar mob normal concede XP y sube a Lv2");
        var saved = repository.GetAsync(record.Id).GetAwaiter().GetResult();
        Check(saved is not null && saved.Level == 2 && saved.AvailableAttributePoints == 3,
            "Combat/XP: subida queda persistida inmediatamente");
        Check(sent.OfType<CombatDebugPacket>().Count() == 3 &&
              sent.OfType<PlayerStatsPacket>().Any(packet => packet.Stats.Level == 2),
            "Combat/XP: servidor devuelve telemetría y stats actualizados");

        var state = new ClientGameState();
        state.Start(new MapLoadPacket(composition.World.Projection("dev-1"), new EntityId(700), record.Id));
        var authoritative = PlayerStatsProjection.Create(player, composition.Systems.Progression);
        state.Apply(authoritative);
        Check(state.Local.Stats == authoritative.Stats,
            "Client: snapshot autoritativo de stats se conserva sin recalcularlo");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
