using NuevoMMO.Contracts;

namespace NuevoMMO.Client;

public sealed class ClientWorldState
{
    private readonly Dictionary<EntityId, EntityProjection> entities = [];
    private readonly Dictionary<EntityId, InterpolationBuffer> buffers = [];
    private bool receivedFull;
    public HandshakeAccepted? Session { get; private set; }
    public MovementPredictor? Predictor { get; private set; }
    public IReadOnlyDictionary<EntityId, EntityProjection> Entities => entities;
    public long LastTick { get; private set; } = -1;
    public double LastSnapshotTime { get; private set; }
    public int MaxVisibleCount { get; private set; }

    public void Start(HandshakeAccepted session)
    {
        Clear(); Session = session;
    }

    public void Apply(WorldSnapshot snapshot, double localTime)
    {
        if (Session is not { } session || snapshot.Correction.Self != session.Self || snapshot.Tick <= LastTick ||
            (!receivedFull && !snapshot.Full)) throw new InvalidDataException("Snapshot fuera de sesión/orden.");
        if (snapshot.Full) { entities.Clear(); buffers.Clear(); receivedFull = true; }
        foreach (var id in snapshot.Despawns) { entities.Remove(id); buffers.Remove(id); }
        foreach (var entity in snapshot.Upserts) entities[entity.Id] = entity;
        if (!entities.ContainsKey(session.Self)) throw new InvalidDataException("El snapshot no contiene al jugador local.");
        Predictor ??= new(session.Map, snapshot.Correction.Position);
        Predictor.Reconcile(snapshot.Correction);
        // Include unchanged entities to anchor their idle position along the server timeline.
        foreach (var entity in entities.Values)
        {
            if (!buffers.TryGetValue(entity.Id, out var buffer)) buffers[entity.Id] = buffer = new();
            buffer.Add(snapshot.Tick, entity.Position);
        }
        LastTick = snapshot.Tick; LastSnapshotTime = localTime; MaxVisibleCount = Math.Max(MaxVisibleCount, entities.Count);
    }

    public WorldPosition SampleRemote(EntityId id, double localTime)
    {
        if (Session is null || !buffers.TryGetValue(id, out var buffer)) return default;
        var elapsedTicks = Math.Clamp((localTime - LastSnapshotTime) * 1000 / Session.Map.TickMilliseconds, 0, 2);
        return buffer.Sample(LastTick - 2 + elapsedTicks);
    }

    public void Clear()
    {
        entities.Clear(); buffers.Clear(); Session = null; Predictor = null; receivedFull = false;
        LastTick = -1; MaxVisibleCount = 0; LastSnapshotTime = 0;
    }
}
