using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.Client;

public sealed class LocalPlayerState
{
    public EntityId Id { get; set; }
    public Vector2Data Position { get; set; }
    public LocalMovementPrediction? Prediction { get; set; }
    public PlayerStatsSnapshot? Stats { get; set; }
}
