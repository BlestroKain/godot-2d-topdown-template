using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed class CharacterListRequest(SessionId session, string sessionToken) : IPacket
{
    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public override string ToString() => $"CharacterListRequest {{ Session = {Session}, SessionToken = [REDACTED] }}";
}
