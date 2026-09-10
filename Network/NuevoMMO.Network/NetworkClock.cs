using System.Diagnostics;

namespace NuevoMMO.Network;

public sealed class NetworkClock
{
    private double offsetMilliseconds;
    private double roundTripMilliseconds;
    private bool initialized;
    public static long Timestamp => Stopwatch.GetTimestamp();
    public double OffsetMilliseconds => offsetMilliseconds;
    public double RoundTripMilliseconds => roundTripMilliseconds;
    public long EstimatedServerTimestamp => Timestamp + (long)(offsetMilliseconds * Stopwatch.Frequency / 1000d);

    public void Observe(PongPacket pong, long clientReceiveTimestamp)
    {
        if (clientReceiveTimestamp < pong.ClientSendTimestamp || pong.ServerSendTimestamp < pong.ServerReceiveTimestamp)
            throw new InvalidDataException("Muestra de reloj inválida.");
        var clientElapsed = ToMilliseconds(clientReceiveTimestamp - pong.ClientSendTimestamp);
        var serverElapsed = ToMilliseconds(pong.ServerSendTimestamp - pong.ServerReceiveTimestamp);
        var rtt = Math.Max(0, clientElapsed - serverElapsed);
        var offset = ToMilliseconds(((pong.ServerReceiveTimestamp - pong.ClientSendTimestamp) +
            (pong.ServerSendTimestamp - clientReceiveTimestamp)) / 2);
        const double smoothing = .125;
        if (!initialized) { roundTripMilliseconds = rtt; offsetMilliseconds = offset; initialized = true; }
        else { roundTripMilliseconds += (rtt - roundTripMilliseconds) * smoothing; offsetMilliseconds += (offset - offsetMilliseconds) * smoothing; }
    }

    private static double ToMilliseconds(long ticks) => ticks * 1000d / Stopwatch.Frequency;
}
