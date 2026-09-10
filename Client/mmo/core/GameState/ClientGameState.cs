using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.Client;

public class ClientGameState
{
    private readonly Dictionary<EntityId, Interpolation> buffers = [];
    private bool receivedFull;
    public SessionState Session { get; } = new();
    public LocalPlayerState Local { get; } = new();
    public EntityStateCache Entities { get; } = new();
    public MapState Map { get; } = new();
    public InventoryState Inventory { get; } = new();
    public PartyState Party { get; } = new();
    public LocalMovementPrediction? Predictor => Local.Prediction;
    public long LastTick { get; private set; } = -1;
    public double LastSnapshotTime { get; private set; }
    public int MaxVisibleCount { get; private set; }

    public void Start(MapLoadPacket packet)
    {
        Clear();
        Session.Character = packet.Character;
        Session.Self = packet.Self;
        Session.Map = packet.Map;
        Map.Projection = packet.Map;
        Local.Id = packet.Self;
    }

    public void Apply(EntityStatePacket snapshot, double localTime)
    {
        if (Session.Map is null || snapshot.Correction.Self != Session.Self || snapshot.Tick <= LastTick ||
            (!receivedFull && !snapshot.Full)) throw new InvalidDataException("Snapshot fuera de sesión/orden.");
        if (snapshot.Full) { Entities.Clear(); buffers.Clear(); receivedFull = true; }
        foreach (var id in snapshot.Despawns) { Entities.Remove(id); buffers.Remove(id); }
        foreach (var entity in snapshot.Upserts) Entities.Upsert(entity);
        if (!Entities.Contains(Session.Self)) throw new InvalidDataException("El snapshot no contiene al jugador local.");
        Local.Prediction ??= new(Session.Map, snapshot.Correction.Position);
        Local.Prediction.Reconcile(snapshot.Correction);
        Local.Position = Local.Prediction.Position;
        foreach (var entity in Entities.All.Values)
        {
            if (!buffers.TryGetValue(entity.Id, out var buffer)) buffers[entity.Id] = buffer = new();
            buffer.Add(snapshot.Tick, entity.Position);
        }
        LastTick = snapshot.Tick;
        LastSnapshotTime = localTime;
        MaxVisibleCount = Math.Max(MaxVisibleCount, Entities.All.Count);
    }

    public Vector2Data SampleRemote(EntityId id, double localTime)
    {
        if (Session.Map is null || !buffers.TryGetValue(id, out var buffer)) return default;
        var elapsedTicks = Math.Clamp((localTime - LastSnapshotTime) * 1000 / Session.Map.TickMilliseconds, 0, 2);
        return buffer.Sample(LastTick - 2 + elapsedTicks);
    }

    public void Clear()
    {
        Entities.Clear();
        buffers.Clear();
        Session.Map = null;
        Local.Prediction = null;
        receivedFull = false;
        LastTick = -1;
        MaxVisibleCount = 0;
        LastSnapshotTime = 0;
    }
}
