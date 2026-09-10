using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record SpawnEntityPacket(EntityState Entity) : IPacket;
