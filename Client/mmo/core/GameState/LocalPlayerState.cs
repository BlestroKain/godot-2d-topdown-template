using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.Client;

public sealed class LocalPlayerState
{
    public EntityId Id { get; set; }
    public Vector2Data Position { get; set; }
    public LocalMovementPrediction? Prediction { get; set; }
    public PlayerStatsSnapshot? Stats { get; set; }
    public TargetState Target { get; } = new();
    public HotbarState Hotbar { get; } = new();
    public EquipmentState Equipment { get; } = new();
    public List<StatusInstance> Status { get; } = [];
    public List<FriendInstance> Friends { get; } = [];
    public ClientPlayer? Entity { get; set; }

    public void ClearRuntime()
    {
        Prediction = null;
        Stats = null;
        Target.Clear();
        Hotbar.Clear();
        Equipment.Clear();
        Status.Clear();
        Friends.Clear();
        Entity = null;
    }
}
