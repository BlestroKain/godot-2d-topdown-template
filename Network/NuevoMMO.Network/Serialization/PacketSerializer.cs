namespace NuevoMMO.Network;

public static class PacketSerializer
{
    public static byte[] Serialize(IPacket packet) => PacketCodec.Encode(packet);
    public static IPacket Deserialize(ReadOnlySpan<byte> data) => PacketCodec.Decode(data);
}
