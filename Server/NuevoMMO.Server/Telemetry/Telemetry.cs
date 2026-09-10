namespace NuevoMMO.Server.Telemetry;

public sealed class ServerMetrics
{
    public double TickTime { get; private set; }
    public double Tps { get; private set; }
    public int PlayersOnline { get; private set; }
    public long PacketsIn { get; private set; }
    public long PacketsOut { get; private set; }
    public double DbLatency { get; private set; }
    public int EntityCount { get; private set; }
    public int MapInstanceCount { get; private set; } = 1;
    public long Memory { get; private set; }
    public long InvalidPackets { get; private set; }

    public void ObserveTick(double tickMilliseconds, int players, int entities, int maps)
    {
        TickTime = tickMilliseconds;
        Tps = tickMilliseconds <= 0 ? 0 : 1000d / Math.Max(tickMilliseconds, 1);
        PlayersOnline = players;
        EntityCount = entities;
        MapInstanceCount = maps;
        Memory = GC.GetTotalMemory(false);
    }

    public void PacketIn() => PacketsIn++;
    public void PacketOut() => PacketsOut++;
    public void Invalid() => InvalidPackets++;
    public void ObserveDb(double milliseconds) => DbLatency = milliseconds;
}

public static class ServerLog
{
    public static void Info(string message) => Console.WriteLine(message);
}
