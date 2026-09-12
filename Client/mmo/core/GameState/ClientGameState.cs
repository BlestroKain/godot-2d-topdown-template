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
    public ChatState Chat { get; } = new();
    public ClientEntityManager WorldEntities { get; } = new();
    public ClientMap? ClientMap { get; private set; }
    public GameFlowState Flow { get; private set; } = GameFlowState.Disconnected;
    public LocalMovementPrediction? Predictor => Local.Prediction;
    public long LastTick { get; private set; } = -1;
    public double LastSnapshotTime { get; private set; }
    public int MaxVisibleCount { get; private set; }

    public void Start(MapLoadPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        var mapTransition = Session.Map is not null && Session.Character == packet.Character && Session.Self == packet.Self;
        if (mapTransition) ResetMapRuntime();
        else Clear();

        Session.Character = packet.Character;
        Session.Self = packet.Self;
        Session.Map = packet.Map;
        Map.Projection = packet.Map;
        Local.Id = packet.Self;
        ClientMap = new ClientMap(packet.Map);
        Flow = GameFlowState.Loading;
        Chat.Append(ChatChannel.System, mapTransition ? "Cambiando de zona…" : "Entrando al mundo…");
    }

    public void Apply(PlayerStatsPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        if (Session.Map is null) throw new InvalidDataException("Stats recibidos fuera de una sesión de mapa.");
        Local.Stats = packet.Stats;
    }

    public void Apply(InventorySnapshotPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        Inventory.Replace(packet.Items.Select((item, index) =>
            new InventorySlotState(index, item.ItemId, item.DefinitionId, item.Quantity)));
        Local.Equipment.Clear();
        var byId = packet.Items.ToDictionary(static item => item.ItemId);
        foreach (var entry in packet.Equipped)
        {
            if (!byId.TryGetValue(entry.ItemId, out var item)) continue;
            Local.Equipment.Equip(entry.Slot, new InventorySlotState(entry.Index, item.ItemId, item.DefinitionId, item.Quantity));
        }
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
        WorldEntities.SyncFromCache(Entities);
        if (WorldEntities.TryGet(Session.Self, out var self) && self is ClientPlayer player)
            Local.Entity = player;
        Flow = GameFlowState.InWorld;
    }

    public Vector2Data SampleRemote(EntityId id, double localTime)
    {
        if (Session.Map is null || !buffers.TryGetValue(id, out var buffer)) return default;
        var elapsedTicks = Math.Clamp((localTime - LastSnapshotTime) * 1000 / Session.Map.TickMilliseconds, 0, 2);
        return buffer.Sample(LastTick - 2 + elapsedTicks);
    }

    /// <summary>
    /// Limpia únicamente estado dependiente del mapa. Inventario, party, chat, stats y demás
    /// estado del mismo personaje sobreviven a un portal; entidades/AOI/predicción no.
    /// </summary>
    private void ResetMapRuntime()
    {
        Entities.Clear();
        buffers.Clear();
        Session.Map = null;
        Local.Prediction = null;
        Local.Position = default;
        Local.Target.Clear();
        Local.Entity = null;
        WorldEntities.ClearMap();
        ClientMap = null;
        Flow = GameFlowState.Loading;
        receivedFull = false;
        LastTick = -1;
        MaxVisibleCount = 0;
        LastSnapshotTime = 0;
    }

    public void Clear()
    {
        Entities.Clear();
        buffers.Clear();
        Session.Map = null;
        Local.Prediction = null;
        Local.Stats = null;
        Inventory.Clear();
        Party.Clear();
        Chat.Clear();
        WorldEntities.ClearMap();
        ClientMap = null;
        Local.ClearRuntime();
        Flow = GameFlowState.Disconnected;
        receivedFull = false;
        LastTick = -1;
        MaxVisibleCount = 0;
        LastSnapshotTime = 0;
    }
}
