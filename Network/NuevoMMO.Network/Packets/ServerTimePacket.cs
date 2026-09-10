namespace NuevoMMO.Network;

public sealed record ServerTimePacket(long ServerTimestamp, long ServerTick) : IPacket;
