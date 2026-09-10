using NuevoMMO.Contracts;
using NuevoMMO.Domain;
using NuevoMMO.Simulation;

namespace NuevoMMO.Application;

/// <summary>Single map coordinator. One lock is the mutable authority boundary for this slice.</summary>
public sealed class WorldRuntime
{
    private readonly object gate = new();
    private readonly WorldOptions options;
    private readonly MapDefinition map;
    private readonly MovementSystem movement;
    private readonly SpatialIndex spatial;
    private readonly ReplicationSystem replication = new();
    private readonly CommandRouter commands = new();
    private readonly Dictionary<ConnectionId, PlayerSession> sessions = [];
    private readonly Dictionary<EntityId, PlayerEntity> entities = [];
    private long nextId;
    private long tick;
    public int TickMilliseconds => options.TickMilliseconds;
    public int PlayerCount { get { lock (gate) return sessions.Count; } }

    public WorldRuntime(WorldOptions options)
    {
        if (!options.Spawn.IsFinite || !float.IsFinite(options.InterestRadius) || options.InterestRadius <= 0 ||
            options.MaxPlayers is < 1 or > 128 || options.Instance.Value <= 0)
            throw new ArgumentException("Opciones de mundo inválidas.");
        this.options = options;
        map = new(options.Map, options.Width, options.Height);
        movement = new(options.Speed, options.TickMilliseconds);
        spatial = new(options.InterestRadius);
        if (map.Clamp(options.Spawn) != options.Spawn) throw new ArgumentException("Spawn fuera del mapa.");
    }

    public HandshakeAccepted Join(ConnectionId connection, string name, bool activate = true)
    {
        if (connection.Value == Guid.Empty || string.IsNullOrWhiteSpace(name) || name.Length > 24 || name.Any(char.IsControl))
            throw new ArgumentException("Nombre de prueba inválido (1–24 caracteres).");
        lock (gate)
        {
            if (sessions.Count >= options.MaxPlayers || sessions.ContainsKey(connection)) throw new InvalidOperationException("Sesión no disponible.");
            var player = new PlayerEntity(new(++nextId), new(Guid.NewGuid()), options.Instance, name.Trim(), options.Spawn);
            sessions.Add(connection, new(connection, player) { State = activate ? SessionState.InWorld : SessionState.Handshaking });
            entities.Add(player.Id, player); spatial.Update(player.Id, player.Position);
            return new(player.Id, player.Character, new(map.Id, options.Instance, map.Width, map.Height, movement.Speed, movement.TickMilliseconds));
        }
    }

    public void Dispatch(ConnectionId connection, IMessage command)
    {
        lock (gate)
        {
            if (!sessions.TryGetValue(connection, out var session)) throw new InvalidOperationException("Sesión inexistente.");
            commands.Dispatch(session, command);
        }
    }

    public void Activate(ConnectionId connection)
    {
        lock (gate)
        {
            if (!sessions.TryGetValue(connection, out var session) || session.State != SessionState.Handshaking)
                throw new InvalidOperationException("Activación de sesión inválida.");
            session.State = SessionState.InWorld;
        }
    }

    public IReadOnlyDictionary<ConnectionId, WorldSnapshot> Step()
    {
        lock (gate)
        {
            tick++;
            foreach (var entity in entities.Values) { movement.Step(entity, map); spatial.Update(entity.Id, entity.Position); }
            return sessions.Where(pair => pair.Value.State == SessionState.InWorld).ToDictionary(pair => pair.Key, pair => replication.Project(pair.Value, tick,
                spatial.Query(pair.Value.Player.Position, options.InterestRadius).Select(id => entities[id]), options.InterestRadius));
        }
    }

    public void Disconnect(ConnectionId connection)
    {
        lock (gate)
        {
            if (!sessions.Remove(connection, out var session)) return;
            session.State = SessionState.Disconnected;
            entities.Remove(session.Player.Id); spatial.Remove(session.Player.Id);
        }
    }
}
