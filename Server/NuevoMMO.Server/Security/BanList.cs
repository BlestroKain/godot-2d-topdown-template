namespace NuevoMMO.Server.Security;

public sealed class BanList
{
    private readonly HashSet<string> usernames = new(StringComparer.OrdinalIgnoreCase);
    public bool IsBanned(string username) => usernames.Contains(username);
    public void Ban(string username) => usernames.Add(username);
}
