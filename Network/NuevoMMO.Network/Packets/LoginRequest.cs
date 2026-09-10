namespace NuevoMMO.Network;

public sealed class LoginRequest(string username, string password) : IPacket
{
    public string Username { get; } = username;
    public string Password { get; } = password;
    public override string ToString() => $"LoginRequest {{ Username = {Username}, Password = [REDACTED] }}";
}
