using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using NuevoMMO.Application;
using NuevoMMO.Client;
using NuevoMMO.Contracts;
using NuevoMMO.Domain;
using NuevoMMO.Protocol;
using NuevoMMO.Server;
using NuevoMMO.Simulation;

int count = 0;
void Check(bool value, string name)
{
    if (!value) throw new Exception("FAIL: " + name);
    count++; Console.WriteLine("PASS: " + name);
}
void Reject(Action action, string name)
{
    try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException)
    { Check(true, name); return; }
    throw new Exception("No rechazado: " + name);
}
async Task Until(Func<bool> ready, string name, int milliseconds = 4000)
{
    var limit = Environment.TickCount64 + milliseconds;
    while (!ready() && Environment.TickCount64 < limit) await Task.Delay(15);
    Check(ready(), name);
}

var mapId = new MapId(Guid.NewGuid());
var options = new WorldOptions(mapId, new(1), 960, 640, new(100, 100), 120, 50, 60, 32);
var projection = new MapProjection(mapId, new(1), 960, 640, 120, 50);
Check(new EntityId(1) != new EntityId(2) && new CharacterId(Guid.NewGuid()).Value != Guid.Empty, "IDs separados y comparables");
Check(typeof(PlayerEntity).Assembly.GetReferencedAssemblies().All(a => !a.Name!.Contains("Godot") && !a.Name.Contains("Protocol")), "Domain sin Godot ni protocolo");
Check(typeof(MovementSystem).Assembly.GetReferencedAssemblies().All(a => !a.Name!.Contains("Godot")), "Simulation sin Godot");
Reject(() => DevelopmentWorldFactory.Create("Production"), "Fixtures rechazadas en Production");
Reject(() => new WorldRuntime(options with { InterestRadius = float.NaN }), "Configuración no finita rechazada");
Reject(() => new WorldRuntime(options with { Spawn = new(-1, 0) }), "Spawn inválido rechazado");

IMessage[] messages = [new HandshakeRequest("Explorador"), new MoveCommand(1, .5f, -1), new Ping(4), new Pong(4),
    new HandshakeRejected("Inválido"), new HandshakeAccepted(new(1), new(Guid.NewGuid()), projection),
    new WorldSnapshot(1, true, new(new(1), new(100, 100), 0), [new(new(1), "Uno", new(100, 100), default, "template.player")], [])];
foreach (var message in messages)
{
    var encoded = PacketCodec.Encode(message);
    Check(PacketCodec.Encode(PacketCodec.Decode(encoded)).SequenceEqual(encoded), "Roundtrip " + message.GetType().Name);
}
var valid = PacketCodec.Encode(new MoveCommand(1, 1, 0));
for (var length = 0; length < valid.Length; length++)
    Reject(() => PacketCodec.Decode(valid[..length]), "Paquete truncado " + length);
var badVersion = valid.ToArray(); badVersion[4] = 99;
Reject(() => PacketCodec.Decode(badVersion), "Versión desconocida");
var badType = valid.ToArray(); badType[6] = 255;
Reject(() => PacketCodec.Decode(badType), "Tipo desconocido");
Reject(() => PacketCodec.Decode([..valid, 0]), "Bytes extra rechazados");
Reject(() => PacketCodec.Decode(PacketCodec.Encode(new MoveCommand(1, float.NaN, 0))), "NaN rechazado en protocolo");
Reject(() => PacketCodec.Encode(new HandshakeRequest(new string('x', 257))), "Texto limitado");
foreach (var size in new[] { -1, 0, PacketCodec.MaxFrameBytes + 1 })
{
    var header = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(header, size);
    using var stream = new MemoryStream(header);
    try { await Frames.ReadAsync(stream, CancellationToken.None); throw new Exception("Frame aceptado."); }
    catch (InvalidDataException) { Check(true, "Longitud inválida rechazada antes de reservar cuerpo: " + size); }
}

var queue = new MovementInputBuffer();
Reject(() => queue.Enqueue(new(2, 1, 0)), "Saltos de input rechazados");
queue.Enqueue(new(1, 1, 0));
Check(queue.LastProcessed == 0, "Recibir input no confirma procesamiento");
Reject(() => queue.Enqueue(new(1, 1, 0)), "Replay de input rechazado");
Reject(() => queue.Enqueue(new(2, 2, 0)), "Dirección adulterada rechazada");
for (int number = 2; number <= 32; number++) queue.Enqueue(new(number, 0, 0));
Reject(() => queue.Enqueue(new(33, 0, 0)), "Backlog acotado");

var player = new PlayerEntity(new(1), new(Guid.NewGuid()), new(1), "Uno", new(100, 100));
var definition = new MapDefinition(mapId, 960, 640);
var simulation = new MovementSystem(120, 50);
player.Inputs.Enqueue(new(1, 1, 1)); simulation.Step(player, definition);
Check(Math.Abs(Math.Sqrt(Math.Pow(player.Position.X - 100, 2) + Math.Pow(player.Position.Y - 100, 2)) - 6) < .001, "Diagonal normalizada por tick");
var after = player.Position; simulation.Step(player, definition);
Check(player.Position == after && player.Velocity == default, "Sin nuevos inputs no hay movimiento repetido");
var predictor = new MovementPredictor(projection, new(100, 100));
var parityPlayer = new PlayerEntity(new(2), new(Guid.NewGuid()), new(1), "Dos", new(100, 100));
var random = new Random(1947);
for (int index = 0; index < 400; index++)
{
    var command = predictor.Predict(random.Next(-1, 2), random.Next(-1, 2));
    parityPlayer.Inputs.Enqueue(new(command.Number, command.X, command.Y)); simulation.Step(parityPlayer, definition);
    if (index % 7 == 0) predictor.Reconcile(new(new(2), parityPlayer.Position, command.Number));
    if (Math.Abs(predictor.Position.X - parityPlayer.Position.X) + Math.Abs(predictor.Position.Y - parityPlayer.Position.Y) > .002)
        throw new Exception("Prediction divergente.");
}
Check(true, "400 pasos aleatorios de paridad servidor/predicción");
var replay = new MovementPredictor(projection, new(100, 100));
replay.Predict(1, 0); replay.Predict(1, 0); replay.Predict(1, 0);
replay.Reconcile(new(new(1), new(50, 100), 1));
Check(replay.Position == new WorldPosition(62, 100) && replay.PendingCount == 2, "Corrección autoritativa + replay de pendientes");
Reject(() => replay.Reconcile(new(new(1), default, 1000)), "ACK del futuro rechazado");
var edge = new MovementPredictor(projection, new(959, 639)); edge.Predict(1, 1);
Check(edge.Position == new WorldPosition(960, 640), "Límites en predicción");
var interpolation = new InterpolationBuffer(); interpolation.Add(1, new(0, 0)); interpolation.Add(3, new(20, 0));
Check(interpolation.Sample(2) == new WorldPosition(10, 0) && interpolation.Sample(99) == new WorldPosition(20, 0), "Interpolación remota sin extrapolación ilimitada");

var world = new WorldRuntime(options);
var stagedConnection = new ConnectionId(Guid.NewGuid());
var stagingWorld = new WorldRuntime(options);
stagingWorld.Join(stagedConnection, "Esperando", activate: false);
Check(stagingWorld.Step().Count == 0, "Handshake incompleto no consume snapshot inicial");
Reject(() => stagingWorld.Dispatch(stagedConnection, new MoveCommand(1, 1, 0)), "Sesión no activada rechaza input");
stagingWorld.Activate(stagedConnection);
Check(stagingWorld.Step()[stagedConnection].Full, "Primer snapshot completo después de activar transporte");
var first = new ConnectionId(Guid.NewGuid()); var second = new ConnectionId(Guid.NewGuid());
var one = world.Join(first, "Uno"); var two = world.Join(second, "Dos");
var snapshot = world.Step();
Check(snapshot[first].Full && snapshot[first].Upserts.Length == 2 && one.Self != two.Self, "Spawn de dos entidades y snapshot inicial");
var state = new ClientWorldState(); state.Start(one); state.Apply(snapshot[first], 0);
Check(state.Entities.Count == 2 && state.Predictor is not null, "Proyección cliente construida desde snapshot");
var idle = world.Step(); state.Apply(idle[first], .05);
Check(!idle[first].Full && idle[first].Upserts.Length == 0 && idle[first].Correction.LastProcessedInput == 0, "Delta vacío en reposo mantiene corrección privada");
Reject(() => state.Apply(idle[first], .1), "Snapshot repetido rechazado");
Reject(() => world.Dispatch(new(Guid.NewGuid()), new MoveCommand(1, 1, 0)), "Comando sin sesión rechazado");
Reject(() => world.Dispatch(first, new HandshakeRequest("Otro")), "Handshake repetido dentro del mundo rechazado");
for (int index = 1; index <= 11; index++) { world.Dispatch(second, new MoveCommand(index, 1, 0)); snapshot = world.Step(); }
Check(snapshot[first].Despawns.Contains(two.Self), "AOI elimina remoto fuera del radio");
for (int index = 12; index <= 22; index++) { world.Dispatch(second, new MoveCommand(index, -1, 0)); snapshot = world.Step(); }
Check(world.Step()[second].Correction.Position == new WorldPosition(100, 100), "Regreso autoritativo al origen");
world.Disconnect(second); snapshot = world.Step();
Check(snapshot[first].Despawns.Contains(two.Self) && world.PlayerCount == 1, "Disconnect elimina entidad y replica despawn");
var rejoined = world.Join(second, "Dos");
Check(rejoined.Self != two.Self && rejoined.Character != two.Character, "Reconexión crea identidad temporal nueva");

var networkWorld = DevelopmentWorldFactory.Create("Test");
var host = new ServerHost(networkWorld, 0);
using var stop = new CancellationTokenSource();
var hostTask = host.RunAsync(stop.Token);
try
{
    using var clientOne = new GameConnection(); using var clientTwo = new GameConnection();
    var receivedOne = new ConcurrentQueue<IMessage>(); var receivedTwo = new ConcurrentQueue<IMessage>();
    clientOne.Message += receivedOne.Enqueue; clientTwo.Message += receivedTwo.Enqueue;
    await clientOne.ConnectAsync("127.0.0.1", host.Port, "Cliente1");
    await clientTwo.ConnectAsync("127.0.0.1", host.Port, "Cliente2");
    await Until(() => receivedOne.OfType<WorldSnapshot>().Any(s => s.Upserts.Length >= 2), "TCP real: dos clientes se ven");
    var idTwo = receivedTwo.OfType<HandshakeAccepted>().Single().Self;
    await clientTwo.SendAsync(new MoveCommand(1, 1, 0));
    await Until(() => receivedTwo.OfType<WorldSnapshot>().Any(s => s.Correction.LastProcessedInput == 1 && s.Correction.Position.X > 240), "TCP real: movimiento confirmado por servidor");
    await clientOne.SendAsync(new Ping(873));
    await Until(() => receivedOne.OfType<Pong>().Any(p => p.Nonce == 873), "Ping/pong");
    clientTwo.Dispose();
    await Until(() => receivedOne.OfType<WorldSnapshot>().Any(s => s.Despawns.Contains(idTwo)), "TCP real: desconexión replica despawn");
    using var reconnected = new GameConnection();
    await reconnected.ConnectAsync("127.0.0.1", host.Port, "Reingreso");
    await Until(() => host.PlayerCount == 2, "TCP real: reconexión");
    using var malformed = new TcpClient(); await malformed.ConnectAsync("127.0.0.1", host.Port);
    await Frames.WriteAsync(malformed.GetStream(), new MoveCommand(1, 1, 0), CancellationToken.None);
    using var timeout = new CancellationTokenSource(2000);
    Check(await Frames.ReadAsync(malformed.GetStream(), timeout.Token) is HandshakeRejected, "TCP real: input antes de handshake rechazado");
}
finally { stop.Cancel(); await hostTask.WaitAsync(TimeSpan.FromSeconds(5)); }
Check(host.PlayerCount == 0, "Cierre ordenado retira todas las sesiones");
Console.WriteLine($"OK: {count} comprobaciones.");
