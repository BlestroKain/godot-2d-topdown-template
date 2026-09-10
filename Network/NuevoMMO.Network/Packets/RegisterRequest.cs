namespace NuevoMMO.Network;

public sealed class RegisterRequest(string username, string password) : IPacket
{
    public string Username { get; } = username;
    public string Password { get; } = password;
    public override string ToString() => $"RegisterRequest {{ Username = {Username}, Password = [REDACTED] }}";
}
