using System.Runtime.CompilerServices;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Systems;
using NuevoMMO.Server.World;

internal static class MultiMapHardeningVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyPersistedSecondaryMapJoin();
        VerifyPortalHandshakeAndPersistence();
        VerifyClientPreservesCharacterStateAcrossMapLoad();
    }

    private static void VerifyPersistedSecondaryMapJoin()
    {
        var mapA = CreateMap("maps.multimap.a", new BoundsData(new(0, 0), new(300, 300)), new(100, 100));
        var mapB = CreateMap("maps.multimap.b", new BoundsData(new(1000, 1000), new(1300, 1300)), new(1050, 1050));
        var world = new WorldRuntime(mapA, Options(), new OscillatingMobPolicy());
        var secondary = world.AddMap(mapB, new MapInstanceId(2));
        var session = AuthenticatedSession(world, "PersistidoB");
        var character = new CharacterId(Guid.NewGuid());

        var player = world.Join(session, new CharacterSpawn(session.Account, character, "PersistidoB", mapB.Id, new(1075, 1080)));

        Expect(player.MapInstanceId == secondary.Id && player.Position == new Vector2Data(1075, 1080),
            "Join restaura MapDefinition y posición del mapa secundario");
        Expect(session.State == PlayerSessionState.WaitingForMap &&
               !world.MapInstance.Entities.TryGet(player.Id, out _) &&
               !secondary.Entities.TryGet(player.Id, out _),
            "WaitingForMap no participa físicamente en ninguna instancia");

        var load = world.PrepareMapLoad(session, "multimap-test");
        Expect(load.Map.Definition == mapB.Id && load.Map.Instance == secondary.Id,
            "MapLoad inicial corresponde al mapa persistido");

        world.Activate(session, secondary.Id);
        Expect(secondary.Entities.TryGet(player.Id, out var activated) && ReferenceEquals(activated, player),
            "MapReady inserta al jugador en la instancia destino");
        var snapshot = world.Step()[session.Connection];
        Expect(snapshot.Full && snapshot.Upserts.Any(entity => entity.Id == player.Id),
            "Primer snapshot del mapa persistido es Full");
    }

    private static void VerifyPortalHandshakeAndPersistence()
    {
        var mapB = CreateMap("maps.portal.b", new BoundsData(new(1000, 1000), new(1300, 1300)), new(1050, 1050));
        var portal = new MapPortalDefinition(
            Guid.NewGuid(),
            new MapShapeDefinition(MapShapeKind.Rectangle, new(100, 100), new(64, 64)),
            mapB.Id,
            new Vector2Data(1100, 1110),
            Direction.Right);
        var mapA = CreateMap(
            "maps.portal.a",
            new BoundsData(new(0, 0), new(300, 300)),
            new(100, 100),
            new MapContentDefinition(portals: [portal]));

        var definitions = new DefinitionRegistry();
        definitions.Register(mapA);
        definitions.Register(mapB);
        var systems = new GameSystems(definitions);
        var world = new WorldRuntime(mapA, Options(), new OscillatingMobPolicy(), systems);
        var secondary = world.AddMap(mapB, new MapInstanceId(2));
        var session = AuthenticatedSession(world, "PortalTester");
        var repository = new InMemoryCharacterRepository();
        var record = repository.CreateAsync(session.Account, "PortalTester", mapA.Id, mapA.Spawn).GetAwaiter().GetResult();
        var player = world.Join(session, new CharacterSpawn(session.Account, record.Id, record.Name, mapA.Id, mapA.Spawn));
        world.PrepareMapLoad(session, "multimap-test");
        world.Activate(session, world.Instance);

        var transitionTick = world.Step();
        Expect(!transitionTick.ContainsKey(session.Connection) && session.State == PlayerSessionState.WaitingForMap,
            "Portal A→B suspende snapshots hasta completar MapReady");
        Expect(player.MapInstanceId == secondary.Id && player.Position == portal.Destination && player.Direction == Direction.Right,
            "Portal traslada estado autoritativo a mapa/posición/facing destino");
        Expect(!world.MapInstance.Entities.TryGet(player.Id, out _) && !secondary.Entities.TryGet(player.Id, out _),
            "Jugador en carga queda fuera de source y destination simulation");

        var pending = world.PendingMapLoads("multimap-test");
        Expect(pending.TryGetValue(session.Connection, out var load) &&
               load.Map.Definition == mapB.Id && load.Map.Instance == secondary.Id,
            "Portal produce MapLoad del destino sin reconectar");

        var persistence = new PersistenceService(repository, mapA, world.MapDefinitionFor);
        persistence.SaveCharacterAsync(player).GetAwaiter().GetResult();
        var saved = repository.GetAsync(record.Id).GetAwaiter().GetResult();
        Expect(saved is not null && saved.MapDefinition == mapB.Id && saved.Position == portal.Destination,
            "Checkpoint del mismo Player persiste MapDefinition y posición de la instancia actual");

        world.Activate(session, secondary.Id);
        var destinationSnapshot = world.Step()[session.Connection];
        Expect(destinationSnapshot.Full && destinationSnapshot.Correction.Position == portal.Destination &&
               destinationSnapshot.Upserts.Any(entity => entity.Id == player.Id),
            "Tras MapReady el primer snapshot de B es Full y contiene al jugador");
    }

    private static void VerifyClientPreservesCharacterStateAcrossMapLoad()
    {
        var mapA = CreateMap("maps.client.a", new BoundsData(new(0, 0), new(300, 300)), new(100, 100));
        var mapB = CreateMap("maps.client.b", new BoundsData(new(1000, 1000), new(1300, 1300)), new(1050, 1050));
        var self = new EntityId(77);
        var character = new CharacterId(Guid.NewGuid());
        var state = new ClientGameState();
        var projectionA = new MapProjection(mapA.Id, new MapInstanceId(1), mapA.VisualKey, mapA.Bounds, 120, 50, "multimap-test");
        var projectionB = new MapProjection(mapB.Id, new MapInstanceId(2), mapB.VisualKey, mapB.Bounds, 120, 50, "multimap-test");
        var stats = new PlayerStatsPacket(new PlayerStatsSnapshot(
            7, 123, 900, 4,
            new(20, 22, 1), new(18, 18, 1), new(16, 17, 1), new(14, 14, 1), new(21, 24, 1),
            700, 720, 220, 240, 9.25f, 2.2f, 6.6f, 5,
            6, 7, 8, 9, 10));

        state.Start(new MapLoadPacket(projectionA, self, character));
        state.Apply(stats);
        state.Chat.Append(ChatChannel.Local, "mensaje que debe sobrevivir");
        var playerA = new PlayerState(self, character, projectionA.Instance, mapA.Spawn, default, Direction.Down,
            new ContentKey("template.player"), "Cliente");
        state.Apply(new EntityStatePacket(1, true, new MovementCorrection(self, mapA.Spawn, 0), [playerA], []), 0);
        Expect(state.Flow == GameFlowState.InWorld && state.Local.Stats == stats.Stats,
            "Cliente entra en A con stats autoritativos");

        state.Start(new MapLoadPacket(projectionB, self, character));
        Expect(state.Flow == GameFlowState.Loading && state.Local.Stats == stats.Stats &&
               state.Chat.Messages.Any(message => message.Text == "mensaje que debe sobrevivir") &&
               state.Entities.All.Count == 0 && state.Predictor is null,
            "Cambio de mapa conserva stats/chat y limpia solo runtime de mapa");

        var rejectedDelta = false;
        try
        {
            state.Apply(new EntityStatePacket(2, false, new MovementCorrection(self, mapB.Spawn, 0), [], []), .05);
        }
        catch (InvalidDataException)
        {
            rejectedDelta = true;
        }
        Expect(rejectedDelta, "Cliente exige Full snapshot después de MapLoad");

        var playerB = new PlayerState(self, character, projectionB.Instance, mapB.Spawn, default, Direction.Right,
            new ContentKey("template.player"), "Cliente");
        state.Apply(new EntityStatePacket(3, true, new MovementCorrection(self, mapB.Spawn, 0), [playerB], []), .1);
        Expect(state.Flow == GameFlowState.InWorld && state.Session.Map?.Instance == projectionB.Instance &&
               state.Local.Stats == stats.Stats,
            "Full snapshot completa transición cliente a B sin perder estado persistente");
    }

    private static MapDefinition CreateMap(string key, BoundsData bounds, Vector2Data spawn, MapContentDefinition? content = null)
        => new(DefinitionId.New(), new ContentKey(key), key, "", true, 1, null,
            new ContentKey(key + ".visual"), bounds, spawn, new Vector2IntData(32, 32), content);

    private static WorldOptions Options()
        => new(new MapInstanceId(1), 120, 40, 50, 96, 32);

    private static PlayerSession AuthenticatedSession(WorldRuntime world, string token)
    {
        var session = world.AddConnection(new ConnectionId(Guid.NewGuid()));
        world.AcceptProtocol(session);
        world.Authenticate(session, new AccountId(Guid.NewGuid()), new SessionId(Guid.NewGuid()), token);
        return session;
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("MultiMapHardeningVerification FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
