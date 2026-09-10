using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed class NetworkSession(SessionId id, ConnectionId connection, TransportPeerId peer)
{
    public SessionId Id { get; } = id;
    public ConnectionId Connection { get; } = connection;
    public TransportPeerId Peer { get; } = peer;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
}
