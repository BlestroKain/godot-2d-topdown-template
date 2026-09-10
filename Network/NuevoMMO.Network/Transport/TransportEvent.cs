namespace NuevoMMO.Network;

public sealed record TransportEvent(TransportEventKind Kind, TransportPeerId Peer, byte Channel,
    ReadOnlyMemory<byte> Payload, string? Error = null);
