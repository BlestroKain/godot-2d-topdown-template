using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed class CharacterSelectRequest(SessionId session, string sessionToken, CharacterId character) : IPacket
{
    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public CharacterId Character { get; } = character;
    public override string ToString() => $"CharacterSelectRequest {{ Session = {Session}, SessionToken = [REDACTED], Character = {Character} }}";
}
