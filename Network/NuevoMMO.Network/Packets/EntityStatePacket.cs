using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record EntityStatePacket(long Tick, bool Full, MovementCorrection Correction,
    EntityState[] Upserts, EntityId[] Despawns) : IPacket;
