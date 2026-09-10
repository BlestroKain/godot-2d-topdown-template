using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed class LoginResult(bool succeeded, string error, AccountId account, SessionId session, string sessionToken) : IPacket
{
    public bool Succeeded { get; } = succeeded;
    public string Error { get; } = error;
    public AccountId Account { get; } = account;
    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public override string ToString() => $"LoginResult {{ Succeeded = {Succeeded}, Error = {Error}, Account = {Account}, Session = {Session}, SessionToken = [REDACTED] }}";
}
