using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.Client;

public sealed class SessionState
{
    public ConnectionId Connection { get; set; }
    public AccountId Account { get; set; }
    public SessionId Session { get; set; }
    public CharacterId Character { get; set; }
    public EntityId Self { get; set; }
    public MapProjection? Map { get; set; }
}
