namespace NuevoMMO.Network;

public sealed class NetworkDiagnostics
{
    public long PacketsIn { get; private set; }
    public long PacketsOut { get; private set; }
    public long InvalidPackets { get; private set; }

    public void RecordInbound() => PacketsIn++;
    public void RecordOutbound() => PacketsOut++;
    public void RecordInvalid() => InvalidPackets++;
}
