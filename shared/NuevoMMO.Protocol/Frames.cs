using System.Buffers.Binary;
using NuevoMMO.Contracts;

namespace NuevoMMO.Protocol;

// Adapted from the project's own GodotMMO Frames: one reader and one writer per stream.
public static class Frames
{
    public static async Task<IMessage> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, cancellationToken);
        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length is <= 0 or > PacketCodec.MaxFrameBytes) throw new InvalidDataException("Frame inválido.");
        var body = new byte[length];
        await stream.ReadExactlyAsync(body, cancellationToken);
        return PacketCodec.Decode(body);
    }

    public static async Task WriteAsync(Stream stream, IMessage message, CancellationToken cancellationToken)
    {
        var body = PacketCodec.Encode(message);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, body.Length);
        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(body, cancellationToken);
    }
}
