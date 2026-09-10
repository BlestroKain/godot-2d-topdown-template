namespace NuevoMMO.Network;

public sealed record PongPacket(long Nonce, long ClientSendTimestamp, long ServerReceiveTimestamp, long ServerSendTimestamp) : IPacket;
