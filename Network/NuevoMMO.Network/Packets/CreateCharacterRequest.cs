using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed class CreateCharacterRequest(SessionId session, string sessionToken, string name) : IPacket
{
    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public string Name { get; } = name;
    public override string ToString() => $"CreateCharacterRequest {{ Session = {Session}, SessionToken = [REDACTED], Name = {Name} }}";
}
