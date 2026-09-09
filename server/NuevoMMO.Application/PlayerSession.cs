using NuevoMMO.Contracts;
using NuevoMMO.Domain;

namespace NuevoMMO.Application;

public enum SessionState { Connected, Handshaking, AuthenticatedPlaceholder, InWorld, Disconnected }

public sealed class PlayerSession(ConnectionId connection, PlayerEntity player)
{
    public ConnectionId Connection { get; } = connection;
    public PlayerEntity Player { get; } = player;
    public SessionState State { get; internal set; } = SessionState.InWorld;
    internal Dictionary<EntityId, EntityProjection> Baseline { get; } = [];
    internal bool HasSnapshot { get; set; }
}
