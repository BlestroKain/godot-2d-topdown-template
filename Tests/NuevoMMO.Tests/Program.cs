using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Editor;
using NuevoMMO.Network;
using NuevoMMO.Server;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;
using NuevoMMO.Server.World;
using NuevoMMO.Server.Security;

int count = 0;
void Check(bool value, string name)
{
    if (!value) throw new Exception("FAIL: " + name);
    count++; Console.WriteLine("PASS: " + name);
}
void Reject(Action action, string name)
{
    try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or KeyNotFoundException)
    { Check(true, name); return; }
    throw new Exception("No rechazado: " + name);
}
async Task Until(Func<bool> ready, string name, int milliseconds = 8000)
{
    var limit = Environment.TickCount64 + milliseconds;
    while (!ready() && Environment.TickCount64 < limit) await Task.Delay(15);
    Check(ready(), name);
}

Check(CanonicalTraditions.IsNovice(DefinitionId.Empty), "Novicio = TraditionId vacío");
Check(CanonicalTraditions.IsValidAtCreate(DefinitionId.Empty), "Create acepta Novicio");
Check(CanonicalTraditions.DisplayName(DefinitionId.Empty) == "Novicio", "DisplayName Novicio");
Check(!CanonicalTraditions.IsValidAtCreate(DefinitionId.New()), "Create rechaza tradición desconocida");
Check(new CreateCharacterRequest(new(Guid.NewGuid()), "token", "Uno").TraditionId.IsEmpty, "CreateCharacterRequest nace Novicio");
Check(AssetCatalog.Key(AssetKind.Item, "potion_hp").Value == "items.potion_hp", "AssetCatalog key de item");
Check(AssetCatalog.RelativePath(AssetKind.Tileset, "grounds") == "tilesets/grounds.png", "AssetCatalog ruta tileset");
Check(AssetCatalog.RelativePath(new ContentKey("items.potion_hp")) == "items/potion_hp.png", "AssetCatalog RelativePath ContentKey");
Check(AssetCatalog.GodotPath(new ContentKey("items.potion_hp")) == "res://resources/items/potion_hp.png", "AssetCatalog ruta Godot");
Check(AssetCatalog.SharedRootFromRepo == "Client/resources", "AssetCatalog raíz compartida editor/Godot");
Check(AssetCatalog.DiskPath("Client/resources", new ContentKey("items.potion_hp"))
    .Replace('/', Path.DirectorySeparatorChar)
    .EndsWith(Path.Combine("Client", "resources", "items", "potion_hp.png"), StringComparison.OrdinalIgnoreCase),
    "AssetCatalog ruta disco compartida");
Check(AssetCatalog.TryKind(new ContentKey("spells.fireball"), out var spellKind) && spellKind == AssetKind.Spell, "AssetCatalog kind spell");
Check(ClientAssetPaths.SharedRootFromRepo == AssetCatalog.SharedRootFromRepo, "Cliente C# misma raíz que el catálogo");
Check(ClientAssetPaths.GodotRoot == "res://resources", "Cliente C# raíz Godot res://resources");
Check(ClientAssetPaths.Godot(new ContentKey("items.potion_hp")) == AssetCatalog.GodotPath(new ContentKey("items.potion_hp")),
    "Cliente C# y catálogo misma ruta Godot");
Check(File.Exists(Path.Combine("Client", "addons", "gloot", "LICENSE")), "GLoot vendorizado conserva licencia");
Check(File.Exists(Path.Combine("Client", "addons", "godot_state_charts", "LICENSE")),
    "Godot State Charts vendorizado conserva licencia");
Check(File.Exists(Path.Combine("Client", "addons", "addons.lock.json")), "Addons Godot fijados por revisión");
Check(File.Exists(Path.Combine("Client", "mmo", "presentation", "inventory", "server_inventory_grid.gd")),
    "Adaptador GLoot autoritativo presente");
Check(File.ReadAllText(Path.Combine("Client", "mmo", "presentation", "ui", "windows", "inventory_window.tscn"))
        .Contains("server_inventory_grid.gd", StringComparison.Ordinal),
    "Ventana de inventario usa el grid GLoot adaptado");

var editorAssets = new AssetLibrary(new EditorConfiguration());
Check(editorAssets.Root.Replace('\\', '/').TrimEnd('/').EndsWith("Client/resources", StringComparison.OrdinalIgnoreCase),
    "Editor AssetLibrary = Client/resources");
Check(Directory.Exists(Path.Combine(editorAssets.Root, AssetCatalog.Folder(AssetKind.Tileset))),
    "Carpeta compartida tilesets existe");
var sharedTileset = AssetCatalog.Key(AssetKind.Tileset, "Ground");
Check(editorAssets.TryResolve(sharedTileset, out var sharedTilesetPath) && File.Exists(sharedTilesetPath),
    "Editor lee tileset desde Client/resources");
Check(File.Exists(AssetCatalog.DiskPath(editorAssets.Root, sharedTileset)),
    "DiskPath del editor apunta al mismo PNG");
Check(ClientAssetPaths.Disk(editorAssets.Root, sharedTileset) == AssetCatalog.DiskPath(editorAssets.Root, sharedTileset),
    "Cliente C# y editor mismo DiskPath");
Check(typeof(GameDefinition).Assembly.GetReferencedAssemblies().All(a => a.Name is not ("GodotSharp" or "ENet-CSharp" or "GodotSharpEditor")), "Core sin Godot ni ENet");
Check(typeof(PacketCodec).Assembly.GetReferencedAssemblies().All(a => a.Name is not ("GodotSharp" or "GodotSharpEditor")), "Network sin Godot");
Check(typeof(Entity).Assembly.GetReferencedAssemblies().All(a => a.Name is not ("GodotSharp" or "GodotSharpEditor")), "Server sin Godot");

var map = new MapDefinition(DefinitionId.New(), new("maps.test"), "Test", "", true, 1, null,
    new("maps.test.visual"), new(new(0, 0), new(960, 640)), new(100, 100), new(32, 32));
var mobDefinition = new MobDefinition(DefinitionId.New(), new("mobs.test"), "Mob", "", true, 1, null, new("template.player"));
Reject(() => new MobDefinition(DefinitionId.New(), new("mobs.bad"), "Mob", "", true, 1, null, default), "VisualKey vacío rechazado");
Reject(() => new MobDefinition(DefinitionId.New(), new("mobs.bad"), "Mob", "", true, 1, null, new("template.player"), DefinitionId.Empty), "LootTableId vacío rechazado");
Check(mobDefinition.LootTableId is null, "LootTableId opcional");
Check(new EntityId(1) != new EntityId(2) && new CharacterId(Guid.NewGuid()).Value != Guid.Empty, "IDs separados y comparables");
Check(ElementMapping.PrimaryFor(Element.Earth) == PrimaryAttributeId.Strength, "Tierra = STR");
Check(ElementMapping.PrimaryFor(Element.Fire) == PrimaryAttributeId.Intelligence, "Fuego = INT");

var package = ContentPackage.Empty("dev-1") with { Maps = [map], Mobs = [mobDefinition] };
Check(package.Validate().Count == 0, "ContentPackage válido");
Check(ContentPackage.FromJson(package.ToJson()).Maps[0].Id == map.Id, "Definition serialization roundtrip");
var registry = new DefinitionRegistry();
package.LoadInto(registry);
Check(registry.Get<MapDefinition>(map.Id).Key.Value == "maps.test", "DefinitionRegistry Get");
Check(registry.GetAll<MobDefinition>().Count == 1, "DefinitionRegistry GetAll");
Reject(() => registry.Register(map), "DefinitionId duplicado rechazado");

IPacket[] packets =
[
    new ConnectRequest("client", ProtocolVersion.Current), new LoginRequest("Uno", "secret"),
    new CharacterListRequest(new(Guid.NewGuid()), "token"), new CreateCharacterRequest(new(Guid.NewGuid()), "token", "Uno"),
    new CharacterSelectRequest(new(Guid.NewGuid()), "token", new(Guid.NewGuid())), new MapReadyRequest(new(1)),
    new MoveRequest(new(1, 1, .5f, -1)), new PingPacket(4, 8), new DisconnectRequest("bye"),
    new ConnectionAccepted(new(Guid.NewGuid()), 1, ProtocolVersion.Current),
    new LoginResult(true, "", new(Guid.NewGuid()), new(Guid.NewGuid()), "token"),
    new CharacterListResult([new(new(Guid.NewGuid()), "Uno", map.Id, new(100, 100))]),
    new CharacterCreated(new(new(Guid.NewGuid()), "Uno", map.Id, new(100, 100))),
    new CharacterSelected(new(Guid.NewGuid())),
    new MapLoadPacket(new(map.Id, new(1), map.VisualKey, map.Bounds, 120, 50, "dev-1"), new(1), new(Guid.NewGuid())),
    new SpawnEntityPacket(new PlayerState(new(1), new(Guid.NewGuid()), new(1), new(100, 100), default, Direction.Down, new("template.player"), "Uno")),
    new DespawnEntityPacket(new(1)), new EntityMovedPacket(new(1), new(101, 100), new(20, 0), 2),
    new EntityStatePacket(1, true, new(new(1), new(100, 100), 0),
        [new PlayerState(new(1), new(Guid.NewGuid()), new(1), new(100, 100), default, Direction.Down, new("template.player"), "Uno")], []),
    new ServerTimePacket(1, 1), new PongPacket(4, 1, 2, 3), new ErrorPacket("x", "y", false)
];
foreach (var packet in packets)
{
    var encoded = PacketCodec.Encode(packet);
    Check(PacketCodec.Encode(PacketCodec.Decode(encoded)).SequenceEqual(encoded), "Roundtrip " + packet.GetType().Name);
}
Check(!PacketCodec.Encode(new LoginRequest("a", "password")).SequenceEqual(PacketCodec.Encode(new LoginRequest("a", "other"))), "Login password participa en el codec");
Check(!new LoginRequest("a", "password").ToString().Contains("password", StringComparison.Ordinal), "LoginRequest no revela password");
var valid = PacketCodec.Encode(new MoveRequest(new(1, 1, 1, 0)));
for (var length = 0; length < valid.Length; length++)
    Reject(() => PacketCodec.Decode(valid[..length]), "Paquete truncado " + length);
var badVersion = valid.ToArray(); badVersion[4] = 99;
Reject(() => PacketCodec.Decode(badVersion), "Versión desconocida");
var badType = valid.ToArray(); badType[6] = 255;
Reject(() => PacketCodec.Decode(badType), "Tipo desconocido");
Reject(() => PacketCodec.Decode([..valid, 0]), "Bytes extra rechazados");
Reject(() => PacketCodec.Decode(PacketCodec.Encode(new MoveRequest(new(1, 1, float.NaN, 0)))), "NaN rechazado en protocolo");
Reject(() => PacketCodec.Encode(new ConnectRequest(new string('x', 257), 1)), "Texto limitado");
foreach (var size in new[] { -1, 0, PacketCodec.MaxPacketBytes + 1 })
{
    var header = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(header, size);
    using var stream = new MemoryStream(header);
    try { await TcpPacketFraming.ReadAsync(stream, CancellationToken.None); throw new Exception("Frame aceptado."); }
    catch (InvalidDataException) { Check(true, "Longitud inválida rechazada antes de reservar cuerpo: " + size); }
}

var queue = new MovementInputBuffer();
Reject(() => queue.Enqueue(new(2, 1, 0, 0)), "Saltos de input rechazados");
queue.Enqueue(new(1, 1, 0, 0));
Check(queue.LastProcessed == 0, "Recibir input no confirma procesamiento");
Reject(() => queue.Enqueue(new(1, 1, 0, 0)), "Replay de input rechazado");
Reject(() => queue.Enqueue(new(2, 2, 2, 0)), "Dirección adulterada rechazada");
for (int number = 2; number <= 32; number++) queue.Enqueue(new(number, 0, 0, 0));
Reject(() => queue.Enqueue(new(33, 0, 0, 0)), "Backlog acotado");

var player = new Player(new(1), new(Guid.NewGuid()), new(Guid.NewGuid()), new(1), new(100, 100), new("template.player"), "Uno");
var simulation = new MovementSystem(120, 50);
player.Inputs.Enqueue(new(1, 1, 1, 1)); simulation.Step(player, map);
Check(Math.Abs(Math.Sqrt(Math.Pow(player.Position.X - 100, 2) + Math.Pow(player.Position.Y - 100, 2)) - 6) < .001, "Diagonal normalizada por tick");
Check(player.Direction is Direction.Right or Direction.Down, "Facing actualizado");
var after = player.Position; simulation.Step(player, map);
Check(player.Position == after && player.Velocity == default, "Sin nuevos inputs no hay movimiento repetido");

var projection = new MapProjection(map.Id, new(1), map.VisualKey, map.Bounds, 120, 50, "dev-1");
var predictor = new LocalMovementPrediction(projection, new(100, 100));
var parityPlayer = new Player(new(2), new(Guid.NewGuid()), new(Guid.NewGuid()), new(1), new(100, 100), new("template.player"), "Dos");
var random = new Random(1947);
for (int index = 0; index < 400; index++)
{
    var command = predictor.Predict(random.Next(-1, 2), random.Next(-1, 2));
    parityPlayer.Inputs.Enqueue(command.Input); simulation.Step(parityPlayer, map);
    if (index % 7 == 0) predictor.Reconcile(new(new(2), parityPlayer.Position, command.Input.Sequence));
    if (Math.Abs(predictor.Position.X - parityPlayer.Position.X) + Math.Abs(predictor.Position.Y - parityPlayer.Position.Y) > .002)
        throw new Exception("Prediction divergente.");
}
Check(true, "400 pasos aleatorios de paridad servidor/predicción");
var replay = new LocalMovementPrediction(projection, new(100, 100));
replay.Predict(1, 0); replay.Predict(1, 0); replay.Predict(1, 0);
replay.Reconcile(new(new(1), new(50, 100), 1));
Check(replay.Position == new Vector2Data(62, 100) && replay.PendingCount == 2, "Corrección autoritativa + replay de pendientes");
Reject(() => replay.Reconcile(new(new(1), default, 1000)), "ACK del futuro rechazado");
var edge = new LocalMovementPrediction(projection, new(959, 639)); edge.Predict(1, 1);
Check(edge.Position == new Vector2Data(960, 640), "Límites en predicción");
var interpolation = new InterpolationBuffer(); interpolation.Add(1, new(0, 0)); interpolation.Add(3, new(20, 0));
Check(interpolation.Sample(2) == new Vector2Data(10, 0) && interpolation.Sample(99) == new Vector2Data(20, 0), "Interpolación remota sin extrapolación ilimitada");

var entities = new EntityRegistry();
entities.Add(player);
Check(entities.Get<Player>(player.Id) == player, "EntityRegistry Get");
Check(entities.Remove(player.Id, out _), "EntityRegistry Remove");
Reject(() => entities.Get(player.Id), "EntityRegistry miss");

var instance = new MapInstance(new(1), map, 60);
instance.Add(new Player(new(9), new(Guid.NewGuid()), new(Guid.NewGuid()), new(1), new(100, 100), new("template.player"), "Nueve"));
Check(instance.Entities.Count == 1, "MapInstance add");

var options = new WorldOptions(new(1), 120, 40, 50, 60, 32);
var world = new WorldRuntime(map, mobDefinition, options, new OscillatingMobPolicy(), new(900, 500));
var staged = world.AddConnection(new(Guid.NewGuid()));
world.AcceptProtocol(staged);
world.Authenticate(staged, new(Guid.NewGuid()), new(Guid.NewGuid()), "token");
world.Join(staged, new(staged.Account, new(Guid.NewGuid()), "Esperando", map.Id, new(100, 100)));
Check(world.Step().Count == 0, "Handshake incompleto no consume snapshot inicial");
Reject(() => world.SubmitMovement(staged, new(1, 1, 1, 0)), "Sesión no activada rechaza input");
world.Activate(staged, world.Instance);
Check(world.Step()[staged.Connection].Full, "Primer snapshot completo después de activar transporte");
world.Disconnect(staged.Connection);

var first = new ConnectionId(Guid.NewGuid()); var second = new ConnectionId(Guid.NewGuid());
var sessionOne = world.AddConnection(first); var sessionTwo = world.AddConnection(second);
void Enter(PlayerSession session, string name)
{
    world.AcceptProtocol(session);
    world.Authenticate(session, new(Guid.NewGuid()), new(Guid.NewGuid()), name);
    world.Join(session, new(session.Account, new(Guid.NewGuid()), name, map.Id, new(100, 100)));
    world.Activate(session, world.Instance);
}
Enter(sessionOne, "Uno"); Enter(sessionTwo, "Dos");
var snapshot = world.Step();
Check(snapshot[first].Full && snapshot[first].Upserts.Length == 2 && sessionOne.Player!.Id != sessionTwo.Player!.Id, "Spawn de dos entidades y snapshot inicial");
var state = new ClientWorldState();
state.Start(new MapLoadPacket(world.Projection("dev-1"), sessionOne.Player!.Id, sessionOne.Character));
var projectedItemId = new ItemInstanceId(Guid.NewGuid());
state.Apply(new InventorySnapshotPacket(
    [new InventoryItemSnapshot(projectedItemId, DefinitionId.New(), 3, 72)], []));
Check(state.Inventory.Slots.Single().Durability == 72,
    "Proyección cliente conserva durabilidad autoritativa del inventario");
var inventoryRevision = state.Inventory.Revision;
var ringOneId = new ItemInstanceId(Guid.NewGuid());
var ringTwoId = new ItemInstanceId(Guid.NewGuid());
state.Apply(new InventorySnapshotPacket(
    [
        new InventoryItemSnapshot(ringOneId, DefinitionId.New(), 1, 20),
        new InventoryItemSnapshot(ringTwoId, DefinitionId.New(), 1, 25)
    ],
    [
        new EquippedItemSnapshot(EquipmentSlot.Ring, 0, ringOneId),
        new EquippedItemSnapshot(EquipmentSlot.Ring, 1, ringTwoId)
    ]));
Check(state.Local.Equipment.Worn.Count == 2 &&
      state.Local.Equipment.IsEquipped(EquipmentSlot.Ring, 0) &&
      state.Local.Equipment.IsEquipped(EquipmentSlot.Ring, 1),
    "Proyección cliente conserva índices múltiples de anillos/trofeos");
Check(state.Inventory.Revision > inventoryRevision,
    "Cada snapshot de inventario confirma una nueva revisión cliente");
state.Apply(snapshot[first], 0);
Check(state.Entities.All.Count == 2 && state.Predictor is not null, "Proyección cliente construida desde snapshot");
var idle = world.Step(); state.Apply(idle[first], .05);
Check(!idle[first].Full && idle[first].Upserts.Length == 0 && idle[first].Correction.LastProcessedInput == 0, "Delta vacío en reposo mantiene corrección privada");
Reject(() => state.Apply(idle[first], .1), "Snapshot repetido rechazado");
Reject(() => world.SubmitMovement(world.AddConnection(new(Guid.NewGuid())), new(1, 0, 1, 0)), "Comando sin mundo rechazado");
for (int index = 1; index <= 11; index++) { world.SubmitMovement(sessionTwo, new(index, index, 1, 0)); snapshot = world.Step(); }
Check(snapshot[first].Despawns.Contains(sessionTwo.Player!.Id), "AOI elimina remoto fuera del radio");
for (int index = 12; index <= 22; index++) { world.SubmitMovement(sessionTwo, new(index, index, -1, 0)); snapshot = world.Step(); }
Check(world.Step()[second].Correction.Position == new Vector2Data(100, 100), "Regreso autoritativo al origen");
var left = world.Disconnect(second);
Check(left is not null, "Disconnect retorna jugador");
snapshot = world.Step();
Check(snapshot[first].Despawns.Contains(sessionTwo.Player!.Id) && world.PlayerCount == 1, "Disconnect elimina entidad y replica despawn");
var rejoin = world.AddConnection(second);
Enter(rejoin, "Dos");
Check(rejoin.Player!.Id != sessionTwo.Player!.Id && rejoin.Character != sessionTwo.Character, "Reconexión crea identidad temporal nueva");

var accounts = new InMemoryAccountRepository();
var characters = new InMemoryCharacterRepository();
var createdAccount = await accounts.CreateAsync("tester", "hash");
var createdCharacter = await characters.CreateAsync(createdAccount.Id, "Heroe", map.Id, new(240, 240));
await characters.SavePositionAsync(createdCharacter.Id, map.Id, new(250, 240));
var loaded = await characters.GetAsync(createdCharacter.Id);
Check(loaded is { Position: { X: 250 } }, "Repository load/save");

var editor = new EditorApplication(new() { Mode = EditorMode.Offline });
editor.Definitions.Register(map);
Check(new MapDefinitionEditor(editor.Definitions).List().Count == 1, "Editor lista MapDefinition");
Check(new ProjectValidator(editor.Definitions).Validate().Count == 0, "Editor valida proyecto vacío de errores");

Reject(() => DevelopmentWorldFactory.Create("Production"), "Production sin game.db rechazado");
Reject(() => DevelopmentWorldFactory.Create("Staging"), "Environment desconocido rechazado");

var composition = DevelopmentWorldFactory.Create("Development");
Check(composition.World.EntityCount >= 1, "Fixture carga mob inicial");

var party = new PartyState();
party.Set(new PartyId(Guid.NewGuid()), new EntityId(1), [new PartyMemberState(new EntityId(1), "Uno", 2, true)]);
Check(party.HasParty && party.Members.Count == 1, "PartyState conserva miembros");
party.Clear();
Check(!party.HasParty, "PartyState.Clear vacía la party");

var nearby = CombatTargeting.NearestMob(
    [
        new MobState(new EntityId(2), mobDefinition.Id, new MapInstanceId(1), new Vector2Data(10, 0), default, Direction.Down, new("template.player"), "Cerca"),
        new MobState(new EntityId(3), mobDefinition.Id, new MapInstanceId(1), new Vector2Data(400, 0), default, Direction.Down, new("template.player"), "Lejos")
    ],
    new Vector2Data(0, 0));
Check(nearby is { DisplayName: "Cerca" }, "CombatTargeting elige el mob más cercano");

var basic = new BasicAttackRequest(new EntityId(9));
Check(PacketCodec.Decode(PacketCodec.Encode(basic)) is BasicAttackRequest decodedBasic && decodedBasic == basic,
    "Network: BasicAttack roundtrip");
var use = new UseTechniqueRequest(CanonicalCombatContent.BasicAttackId, new EntityId(9), new Vector2Data(1, 2));
Check(PacketCodec.Decode(PacketCodec.Encode(use)) is UseTechniqueRequest decodedUse && decodedUse == use,
    "Network: UseTechnique roundtrip");
var interact = new InteractRequest(new EntityId(4));
Check(PacketCodec.Decode(PacketCodec.Encode(interact)) is InteractRequest decodedInteract && decodedInteract == interact,
    "Network: Interact roundtrip");
var inventorySnapshot = new InventorySnapshotPacket(
    [new InventoryItemSnapshot(new ItemInstanceId(Guid.NewGuid()), DefinitionId.New(), 2, 10)],
    [new EquippedItemSnapshot(EquipmentSlot.Weapon, 0, new ItemInstanceId(Guid.NewGuid()))]);
inventorySnapshot = inventorySnapshot with
{
    Equipped = [new EquippedItemSnapshot(EquipmentSlot.Weapon, 0, inventorySnapshot.Items[0].ItemId)]
};
Check(PacketCodec.Decode(PacketCodec.Encode(inventorySnapshot)) is InventorySnapshotPacket decodedInventory
    && decodedInventory.Items[0].Quantity == 2
    && decodedInventory.Equipped[0].Slot == EquipmentSlot.Weapon,
    "Network: InventorySnapshot roundtrip");
var equip = new EquipItemRequest(inventorySnapshot.Items[0].ItemId);
Check(PacketCodec.Decode(PacketCodec.Encode(equip)) is EquipItemRequest decodedEquip && decodedEquip == equip,
    "Network: EquipItem roundtrip");
var unequip = new UnequipItemRequest(inventorySnapshot.Items[0].ItemId);
Check(PacketCodec.Decode(PacketCodec.Encode(unequip)) is UnequipItemRequest decodedUnequip && decodedUnequip == unequip,
    "Network: UnequipItem roundtrip");
var moveInventory = new MoveInventoryItemRequest(inventorySnapshot.Items[0].ItemId, 0);
Check(PacketCodec.Decode(PacketCodec.Encode(moveInventory)) is MoveInventoryItemRequest decodedMoveInventory && decodedMoveInventory == moveInventory,
    "Network: MoveInventoryItem roundtrip");
Check(PacketRegistry.Describe(moveInventory) == (PacketId.MoveInventoryItemRequest, PacketDirection.ClientToServer),
    "Network: MoveInventoryItem registrado cliente a servidor");
Reject(() => PacketCodec.Encode(moveInventory with { TargetIndex = -1 }),
    "Network: MoveInventoryItem rechaza índice negativo");

var eventRuntime = new EventRuntime(new DefinitionRegistry());
var pageListId = Guid.NewGuid();
var page = new EventPageDefinition(
    Guid.NewGuid(),
    EventTrigger.Action,
    commandLists: new Dictionary<Guid, EventCommandDefinition[]>
    {
        [pageListId] =
        [
            new EventCommandDefinition(Guid.NewGuid(), EventCommandKind.Dialogue, text: new Dictionary<string, string> { ["text"] = "Hola" })
        ]
    },
    rootCommandListId: pageListId);
var evt = new EventDefinition(
    DefinitionId.New(), new ContentKey("events.hello"), "Hola", string.Empty, true, 1, null,
    EventScope.Common, pages: [page]);
var eventRegistry = new DefinitionRegistry();
eventRegistry.Register(evt);
eventRuntime = new EventRuntime(eventRegistry);
var eventPlayer = new Player(new EntityId(80), new AccountId(Guid.NewGuid()), new CharacterId(Guid.NewGuid()),
    new MapInstanceId(1), new Vector2Data(10, 10), new ContentKey("template.player"), "Eventista");
var spoken = eventRuntime.TryTrigger(eventPlayer, evt, EventTrigger.Action, 0);
Check(spoken.Success && spoken.Log.Contains("Hola"), "EventRuntime ejecuta diálogo de la página activa");

var combatHarness = DevelopmentWorldFactory.Create("Test");
var combatSession = combatHarness.World.AddConnection(new ConnectionId(Guid.NewGuid()));
combatHarness.World.AcceptProtocol(combatSession);
combatHarness.World.Authenticate(combatSession, new AccountId(Guid.NewGuid()), new SessionId(Guid.NewGuid()), "token");
combatHarness.World.Join(combatSession, new CharacterSpawn(
    combatSession.Account, new CharacterId(Guid.NewGuid()), "Atacante", combatHarness.World.Map.Id, combatHarness.World.Map.Spawn));
combatHarness.World.Activate(combatSession, combatHarness.World.Instance);
var dummy = combatHarness.World.MapInstance.Entities.All.OfType<Mob>().First(mob => mob.DefinitionId == TrainingDummyFixture.DefinitionId);
dummy.MoveTo(combatSession.Player!.Position, default);
var attack = combatHarness.World.BasicAttack(combatSession, dummy.Id);
Check(attack.Success && attack.Actions.Any(static action => action.Damage is { AppliedDamage: > 0 }),
    "BasicAttack usa TechniqueSystem contra el dummy");
var interactNone = combatHarness.World.Interact(combatSession, default);
Check(!interactNone.Success, "Interact sin objetivo cercano falla de forma controlada");
combatHarness.World.Disconnect(combatSession.Connection);

var mapEventListId = Guid.NewGuid();
var mapPage = new EventPageDefinition(
    Guid.NewGuid(),
    EventTrigger.MapEnter,
    commandLists: new Dictionary<Guid, EventCommandDefinition[]>
    {
        [mapEventListId] =
        [
            new EventCommandDefinition(Guid.NewGuid(), EventCommandKind.ShowNotification,
                text: new Dictionary<string, string> { ["text"] = "Entraste" })
        ]
    },
    rootCommandListId: mapEventListId);
var mapEvent = new EventDefinition(
    DefinitionId.New(), new ContentKey("events.map_enter"), "Entrada", string.Empty, true, 1, null,
    EventScope.Map,
    new EventPlacementDefinition(map.Id, new Vector2Data(100, 100)),
    pages: [mapPage]);
var pulseRegistry = new DefinitionRegistry();
pulseRegistry.Register(mapEvent);
var pulseRuntime = new EventRuntime(pulseRegistry);
var pulsePlayer = new Player(new EntityId(81), new AccountId(Guid.NewGuid()), new CharacterId(Guid.NewGuid()),
    new MapInstanceId(1), new Vector2Data(100, 100), new ContentKey("template.player"), "Viajero");
var pulsed = pulseRuntime.Pulse(pulsePlayer, map, 0);
Check(pulsed.Any(static result => result.Success && result.Log.Contains("Entraste")), "EventRuntime dispara MapEnter una vez");
Check(pulseRuntime.Pulse(pulsePlayer, map, 50).Count == 0, "EventRuntime no repite MapEnter");
var host = new ServerHost(composition.World, composition.Persistence, composition.Dispatcher, 0);
using var stop = new CancellationTokenSource();
var hostTask = host.RunAsync(stop.Token);
try
{
    using var clientOne = new GameConnection(); using var clientTwo = new GameConnection();
    var receivedOne = new ConcurrentQueue<IPacket>(); var receivedTwo = new ConcurrentQueue<IPacket>();
    clientOne.Message += receivedOne.Enqueue; clientTwo.Message += receivedTwo.Enqueue;
    await clientOne.ConnectAsync("127.0.0.1", host.Port, "Cliente1");
    await clientTwo.ConnectAsync("127.0.0.1", host.Port, "Cliente2");
    await Until(() => receivedOne.OfType<EntityStatePacket>().Any(s => s.Upserts.Length >= 2), "TCP real: dos clientes se ven");
    var idTwo = receivedTwo.OfType<MapLoadPacket>().Single().Self;
    await clientTwo.SendAsync(new MoveRequest(new(1, 1, 1, 0)));
    await Until(() => receivedTwo.OfType<EntityStatePacket>().Any(s => s.Correction.LastProcessedInput == 1 && s.Correction.Position.X > 240), "TCP real: movimiento confirmado por servidor");
    await clientOne.SendAsync(new PingPacket(873, NetworkClock.Timestamp));
    await Until(() => receivedOne.OfType<PongPacket>().Any(p => p.Nonce == 873), "Ping/pong");
    clientTwo.Dispose();
    await Until(() => receivedOne.OfType<EntityStatePacket>().Any(s => s.Despawns.Contains(idTwo)), "TCP real: desconexión replica despawn");
    using var reconnected = new GameConnection();
    await reconnected.ConnectAsync("127.0.0.1", host.Port, "Reingreso");
    await Until(() => host.PlayerCount == 2, "TCP real: reconexión");
    using var malformed = new TcpClient(); await malformed.ConnectAsync("127.0.0.1", host.Port);
    await TcpPacketFraming.WriteAsync(malformed.GetStream(), new MoveRequest(new(1, 1, 1, 0)), CancellationToken.None);
    using var timeout = new CancellationTokenSource(2000);
    Check(await TcpPacketFraming.ReadAsync(malformed.GetStream(), timeout.Token) is ErrorPacket, "TCP real: input antes de handshake rechazado");
}
finally { stop.Cancel(); await hostTask.WaitAsync(TimeSpan.FromSeconds(5)); }
Check(host.PlayerCount == 0, "Cierre ordenado retira todas las sesiones");

Check(!typeof(GameDefinition).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).Contains("Node"), "Definitions no exponen nodos");
Console.WriteLine($"OK: {count} comprobaciones.");
