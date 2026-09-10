namespace NuevoMMO.Server.Configuration;

public sealed record ServerConfiguration(string Environment, string Host, int Port, int TickMilliseconds)
{
    public static ServerConfiguration Development(int port = 7777) => new("Development", "127.0.0.1", port, 50);
}
