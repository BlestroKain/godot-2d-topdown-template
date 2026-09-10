using NuevoMMO.Network;

namespace NuevoMMO.Client;

public sealed class Reconciliation
{
    public void Apply(LocalMovementPrediction prediction, MovementCorrection correction) => prediction.Reconcile(correction);
}
