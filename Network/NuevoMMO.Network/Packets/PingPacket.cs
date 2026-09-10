namespace NuevoMMO.Network;

public sealed record PingPacket(long Nonce, long ClientSendTimestamp) : IPacket;
