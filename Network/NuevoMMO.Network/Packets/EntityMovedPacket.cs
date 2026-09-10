using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record EntityMovedPacket(EntityId Entity, Vector2Data Position, Vector2Data Velocity, long ServerTick) : IPacket;
