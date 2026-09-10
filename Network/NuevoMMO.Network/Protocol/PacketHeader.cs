namespace NuevoMMO.Network;

public readonly record struct PacketHeader(ushort Version, PacketId PacketId, uint PayloadLength)
{
    public const int Size = 12;
}
