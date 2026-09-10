using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record DespawnEntityPacket(EntityId Entity) : IPacket;
