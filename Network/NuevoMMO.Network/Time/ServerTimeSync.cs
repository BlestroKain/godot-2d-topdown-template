namespace NuevoMMO.Network;

public sealed class ServerTimeSync(NetworkClock clock)
{
    public NetworkClock Clock { get; } = clock;
    public long ServerTick { get; private set; }
    public long ServerTimestamp { get; private set; }

    public void Apply(ServerTimePacket packet)
    {
        if (packet.ServerTick < 0) throw new InvalidDataException("Tick de servidor inválido.");
        ServerTick = packet.ServerTick;
        ServerTimestamp = packet.ServerTimestamp;
    }
}
