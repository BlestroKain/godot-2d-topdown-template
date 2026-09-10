using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class LocalPlayerState
{
    public EntityId Id { get; set; }
    public Vector2Data Position { get; set; }
    public LocalMovementPrediction? Prediction { get; set; }
}
