namespace NuevoMMO.Server.Security;

public sealed record BanEntry(
    string Username,
    string Reason,
    long CreatedAtMilliseconds,
    long? ExpiresAtMilliseconds,
    string IssuedBy)
{
    public bool IsExpired(long nowMilliseconds)
        => ExpiresAtMilliseconds is { } expires && nowMilliseconds >= expires;
}

/// <summary>
/// Lista runtime de sanciones. La persistencia, si se habilita, debe vivir detrás de un repositorio;
/// esta clase solo representa y consulta el estado autoritativo cargado en memoria.
/// </summary>
public sealed class BanList
{
    private readonly object gate = new();
    private readonly Dictionary<string, BanEntry> usernames = new(StringComparer.OrdinalIgnoreCase);

    public bool IsBanned(string username)
        => IsBanned(username, long.MaxValue, out _);

    public bool IsBanned(string username, long nowMilliseconds, out BanEntry? entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(username)) return false;
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));

        lock (gate)
        {
            if (!usernames.TryGetValue(username.Trim(), out var current)) return false;
            if (nowMilliseconds != long.MaxValue && current.IsExpired(nowMilliseconds))
            {
                usernames.Remove(username.Trim());
                return false;
            }
            entry = current;
            return true;
        }
    }

    /// <summary>Compatibilidad: crea un ban permanente sin contexto.</summary>
    public void Ban(string username)
        => Ban(username, "Sin motivo especificado.", 0, null, "system");

    public BanEntry Ban(string username, string reason, long nowMilliseconds, long? expiresAtMilliseconds, string issuedBy)
    {
        var normalized = NormalizeUsername(username);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Motivo de ban vacío.", nameof(reason));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (expiresAtMilliseconds is { } expires && expires <= nowMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(expiresAtMilliseconds), "La expiración debe ser futura.");
        if (string.IsNullOrWhiteSpace(issuedBy)) throw new ArgumentException("Emisor vacío.", nameof(issuedBy));

        var entry = new BanEntry(normalized, reason.Trim(), nowMilliseconds, expiresAtMilliseconds, issuedBy.Trim());
        lock (gate) usernames[normalized] = entry;
        return entry;
    }

    public bool Unban(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        lock (gate) return usernames.Remove(username.Trim());
    }

    public IReadOnlyList<BanEntry> Active(long nowMilliseconds)
    {
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        lock (gate)
        {
            var expired = usernames.Where(pair => pair.Value.IsExpired(nowMilliseconds)).Select(pair => pair.Key).ToArray();
            foreach (var key in expired) usernames.Remove(key);
            return usernames.Values.OrderBy(entry => entry.Username, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    private static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Usuario vacío.", nameof(username));
        return username.Trim();
    }
}
