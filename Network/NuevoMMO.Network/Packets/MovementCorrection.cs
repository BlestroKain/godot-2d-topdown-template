using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record MovementCorrection(EntityId Self, Vector2Data Position, long LastProcessedInput);
