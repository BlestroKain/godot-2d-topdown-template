using System.Buffers.Binary;

namespace NuevoMMO.Network;

public static class TcpPacketFraming
{
    public static async Task<IPacket> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var sizeBytes = new byte[4];
        await stream.ReadExactlyAsync(sizeBytes, cancellationToken);
        var size = BinaryPrimitives.ReadInt32BigEndian(sizeBytes);
        if (size is < PacketHeader.Size or > PacketCodec.MaxPacketBytes) throw new InvalidDataException("Frame TCP inválido.");
        var packet = new byte[size]; await stream.ReadExactlyAsync(packet, cancellationToken);
        return PacketCodec.Decode(packet);
    }

    public static async Task WriteAsync(Stream stream, IPacket packet, CancellationToken cancellationToken)
    {
        var encoded = PacketCodec.Encode(packet);
        var sizeBytes = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(sizeBytes, encoded.Length);
        await stream.WriteAsync(sizeBytes, cancellationToken); await stream.WriteAsync(encoded, cancellationToken);
    }
}
