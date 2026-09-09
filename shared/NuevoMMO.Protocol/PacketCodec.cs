using System.Text;
using NuevoMMO.Contracts;

namespace NuevoMMO.Protocol;

/// <summary>Explicit bounded binary schema. Intentionally incompatible with the old prototype.</summary>
public static class PacketCodec
{
    public const int MaxFrameBytes = 65536;
    public const int MaxEntities = 128;
    public const ushort Version = 1;
    private const uint Magic = 0x4F4D4D4E; // NMMO, little endian.
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static byte[] Encode(IMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Utf8, true);
        writer.Write(Magic);
        writer.Write(Version);
        switch (message)
        {
            case HandshakeRequest hello:
                writer.Write((byte)1); WriteText(writer, hello.Name); break;
            case MoveCommand move:
                writer.Write((byte)2); writer.Write(move.Number); writer.Write(move.X); writer.Write(move.Y); break;
            case Ping ping:
                writer.Write((byte)3); writer.Write(ping.Nonce); break;
            case HandshakeAccepted accepted:
                writer.Write((byte)64); writer.Write(accepted.Self.Value);
                writer.Write(accepted.Character.Value.ToByteArray());
                var map = accepted.Map;
                writer.Write(map.Id.Value.ToByteArray()); writer.Write(map.Instance.Value);
                writer.Write(map.Width); writer.Write(map.Height); writer.Write(map.MovementSpeed); writer.Write(map.TickMilliseconds);
                break;
            case HandshakeRejected rejected:
                writer.Write((byte)65); WriteText(writer, rejected.Reason); break;
            case WorldSnapshot snapshot:
                writer.Write((byte)66); writer.Write(snapshot.Tick); writer.Write(snapshot.Full);
                writer.Write(snapshot.Correction.Self.Value); WritePosition(writer, snapshot.Correction.Position);
                writer.Write(snapshot.Correction.LastProcessedInput);
                WriteCount(writer, snapshot.Upserts.Length);
                foreach (var entity in snapshot.Upserts)
                {
                    writer.Write(entity.Id.Value); WriteText(writer, entity.Name); WritePosition(writer, entity.Position);
                    WritePosition(writer, entity.Velocity); WriteText(writer, entity.VisualKey);
                }
                WriteCount(writer, snapshot.Despawns.Length);
                foreach (var id in snapshot.Despawns) writer.Write(id.Value);
                break;
            case Pong pong:
                writer.Write((byte)67); writer.Write(pong.Nonce); break;
            default: throw new InvalidDataException("Tipo de mensaje no permitido.");
        }
        if (stream.Length > MaxFrameBytes) throw new InvalidDataException("Frame demasiado grande.");
        return stream.ToArray();
    }

    public static IMessage Decode(byte[] data)
    {
        if (data.Length is < 7 or > MaxFrameBytes) throw new InvalidDataException("Tamaño de frame inválido.");
        try
        {
            using var stream = new MemoryStream(data, false);
            using var reader = new BinaryReader(stream, Utf8);
            if (reader.ReadUInt32() != Magic || reader.ReadUInt16() != Version)
                throw new InvalidDataException("Protocolo incompatible.");
            IMessage message = reader.ReadByte() switch
            {
                1 => new HandshakeRequest(ReadText(reader)),
                2 => new MoveCommand(reader.ReadInt64(), ReadFinite(reader), ReadFinite(reader)),
                3 => new Ping(reader.ReadInt64()),
                64 => ReadAccepted(reader),
                65 => new HandshakeRejected(ReadText(reader)),
                66 => ReadSnapshot(reader),
                67 => new Pong(reader.ReadInt64()),
                _ => throw new InvalidDataException("Tipo de paquete desconocido.")
            };
            if (stream.Position != stream.Length) throw new InvalidDataException("Bytes sobrantes.");
            return message;
        }
        catch (Exception exception) when (exception is EndOfStreamException or ArgumentException or OverflowException)
        {
            throw new InvalidDataException("Paquete truncado o mal formado.", exception);
        }
    }

    private static HandshakeAccepted ReadAccepted(BinaryReader reader)
    {
        var self = new EntityId(reader.ReadInt64());
        var character = new CharacterId(ReadGuid(reader));
        var map = new MapProjection(new(ReadGuid(reader)), new(reader.ReadInt64()),
            ReadFinite(reader), ReadFinite(reader), ReadFinite(reader), reader.ReadInt32());
        if (self.Value <= 0 || character.Value == Guid.Empty || map.Id.Value == Guid.Empty || map.Instance.Value <= 0 ||
            map.Width is <= 0 or > 100000 || map.Height is <= 0 or > 100000 ||
            map.MovementSpeed is <= 0 or > 10000 || map.TickMilliseconds is < 10 or > 1000)
            throw new InvalidDataException("Metadatos de mundo inválidos.");
        return new(self, character, map);
    }

    private static WorldSnapshot ReadSnapshot(BinaryReader reader)
    {
        var tick = reader.ReadInt64();
        var fullByte = reader.ReadByte();
        if (fullByte > 1) throw new InvalidDataException("Booleano inválido.");
        var correction = new MovementCorrection(new(reader.ReadInt64()), ReadPosition(reader), reader.ReadInt64());
        var upserts = new EntityProjection[ReadCount(reader)];
        var seen = new HashSet<EntityId>();
        for (var index = 0; index < upserts.Length; index++)
        {
            var entity = new EntityProjection(new(reader.ReadInt64()), ReadText(reader), ReadPosition(reader), ReadPosition(reader), ReadText(reader));
            if (entity.Id.Value <= 0 || !seen.Add(entity.Id)) throw new InvalidDataException("Entidad repetida o inválida.");
            upserts[index] = entity;
        }
        var despawns = new EntityId[ReadCount(reader)];
        for (var index = 0; index < despawns.Length; index++)
        {
            despawns[index] = new(reader.ReadInt64());
            if (despawns[index].Value <= 0 || !seen.Add(despawns[index])) throw new InvalidDataException("Despawn inválido.");
        }
        if (tick < 0 || correction.Self.Value <= 0 || correction.LastProcessedInput < 0)
            throw new InvalidDataException("Corrección inválida.");
        return new(tick, fullByte == 1, correction, upserts, despawns);
    }

    private static Guid ReadGuid(BinaryReader reader) => new(reader.ReadBytes(16));
    private static float ReadFinite(BinaryReader reader)
    {
        var number = reader.ReadSingle();
        return float.IsFinite(number) ? number : throw new InvalidDataException("Valor no finito.");
    }
    private static WorldPosition ReadPosition(BinaryReader reader) => new(ReadFinite(reader), ReadFinite(reader));
    private static void WritePosition(BinaryWriter writer, WorldPosition position) { writer.Write(position.X); writer.Write(position.Y); }
    private static void WriteCount(BinaryWriter writer, int count)
    {
        if (count is < 0 or > MaxEntities) throw new InvalidDataException("Demasiadas entidades.");
        writer.Write((ushort)count);
    }
    private static int ReadCount(BinaryReader reader)
    {
        int count = reader.ReadUInt16();
        return count <= MaxEntities ? count : throw new InvalidDataException("Demasiadas entidades.");
    }
    private static void WriteText(BinaryWriter writer, string text)
    {
        var bytes = Utf8.GetBytes(text);
        if (bytes.Length > 256) throw new InvalidDataException("Texto demasiado largo.");
        writer.Write((ushort)bytes.Length); writer.Write(bytes);
    }
    private static string ReadText(BinaryReader reader)
    {
        int length = reader.ReadUInt16();
        if (length > 256) throw new InvalidDataException("Texto demasiado largo.");
        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length) throw new EndOfStreamException();
        return Utf8.GetString(bytes);
    }
}
