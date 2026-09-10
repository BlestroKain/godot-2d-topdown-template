using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record MapLoadPacket(MapProjection Map, EntityId Self, CharacterId Character) : IPacket;
