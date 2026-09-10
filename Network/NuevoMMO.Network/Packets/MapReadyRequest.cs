using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record MapReadyRequest(MapInstanceId Instance) : IPacket;
