using System.Text;
using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed class PacketWriter : IDisposable
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly MemoryStream stream = new();
    private readonly BinaryWriter writer;
    public PacketWriter() => writer = new(stream, Utf8, true);
    public void Write(bool value) => writer.Write(value);
    public void Write(byte value) => writer.Write(value);
    public void Write(ushort value) => writer.Write(value);
    public void Write(uint value) => writer.Write(value);
    public void Write(int value) => writer.Write(value);
    public void Write(long value) => writer.Write(value);
    public void Write(float value) { if (!float.IsFinite(value)) throw new InvalidDataException("Número no finito."); writer.Write(value); }
    public void Write(Guid value) => writer.Write(value.ToByteArray());
    public void Write(Vector2Data value) { Write(value.X); Write(value.Y); }
    public void Write(ContentKey value) => Write(value.Value, 128);
    public void Write(string value, int maxBytes = 512)
    {
        var bytes = Utf8.GetBytes(value ?? throw new ArgumentNullException(nameof(value)));
        if (bytes.Length > maxBytes || bytes.Length > ushort.MaxValue) throw new InvalidDataException("Texto demasiado largo.");
        writer.Write((ushort)bytes.Length); writer.Write(bytes);
    }
    public byte[] ToArray() => stream.ToArray();
    public void Dispose() { writer.Dispose(); stream.Dispose(); }
}

public sealed class PacketReader : IDisposable
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly MemoryStream stream;
    private readonly BinaryReader reader;
    public PacketReader(ReadOnlySpan<byte> bytes) { stream = new(bytes.ToArray(), false); reader = new(stream, Utf8); }
    public bool ReadBool() => reader.ReadByte() switch { 0 => false, 1 => true, _ => throw new InvalidDataException("Booleano inválido.") };
    public byte ReadByte() => reader.ReadByte();
    public ushort ReadUInt16() => reader.ReadUInt16();
    public uint ReadUInt32() => reader.ReadUInt32();
    public int ReadInt32() => reader.ReadInt32();
    public long ReadInt64() => reader.ReadInt64();
    public float ReadSingle() { var value = reader.ReadSingle(); return float.IsFinite(value) ? value : throw new InvalidDataException("Número no finito."); }
    public Guid ReadGuid() { var bytes = reader.ReadBytes(16); return bytes.Length == 16 ? new Guid(bytes) : throw new EndOfStreamException(); }
    public Vector2Data ReadVector2() => new(ReadSingle(), ReadSingle());
    public ContentKey ReadContentKey() => new(ReadString(128));
    public string ReadString(int maxBytes = 512)
    {
        var length = reader.ReadUInt16();
        if (length > maxBytes) throw new InvalidDataException("Texto demasiado largo.");
        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length) throw new EndOfStreamException();
        return Utf8.GetString(bytes);
    }
    public void EnsureComplete() { if (stream.Position != stream.Length) throw new InvalidDataException("Bytes sobrantes."); }
    public void Dispose() { reader.Dispose(); stream.Dispose(); }
}
