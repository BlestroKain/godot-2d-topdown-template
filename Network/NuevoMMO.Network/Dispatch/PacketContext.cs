using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record PacketContext(TransportPeerId PeerId, ConnectionId Connection, SessionId? Session,
    DateTimeOffset ReceivedAt, byte DeliveryChannel);
