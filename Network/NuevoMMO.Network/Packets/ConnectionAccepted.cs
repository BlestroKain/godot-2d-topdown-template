using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record ConnectionAccepted(ConnectionId Connection, long ServerTimestamp, ushort ProtocolVersion) : IPacket;
