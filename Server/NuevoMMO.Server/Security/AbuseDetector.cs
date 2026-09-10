namespace NuevoMMO.Server.Security;

public enum AbuseDecision : byte
{
    Allowed = 0,
    RateLimited = 1
}

public readonly record struct AbuseCheck(
    AbuseDecision Decision,
    int Count,
    int Limit,
    long RetryAfterMilliseconds)
{
    public bool Allowed => Decision == AbuseDecision.Allowed;
}

/// <summary>
/// Limitador autoritativo por clave/acción basado en ventana deslizante. No desconecta ni sanciona
/// por sí solo: devuelve una decisión para que ServerHost/handler aplique la política correspondiente.
/// </summary>
public sealed class AbuseDetector
{
    private readonly object gate = new();
    private readonly Dictionary<string, Queue<long>> events = new(StringComparer.Ordinal);

    /// <summary>Compatibilidad con la comprobación antigua sin estado.</summary>
    public bool IsBurst(int packets, int limit)
    {
        if (packets < 0) throw new ArgumentOutOfRangeException(nameof(packets));
        if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit));
        return packets > limit;
    }

    public AbuseCheck Check(string key, long nowMilliseconds, int limit, int windowMilliseconds)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Clave de rate limit vacía.", nameof(key));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit));
        if (windowMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(windowMilliseconds));

        lock (gate)
        {
            if (!events.TryGetValue(key, out var timestamps)) events[key] = timestamps = new Queue<long>();
            var threshold = nowMilliseconds - windowMilliseconds;
            while (timestamps.Count > 0 && timestamps.Peek() <= threshold) timestamps.Dequeue();

            if (timestamps.Count >= limit)
            {
                var retryAfter = Math.Max(1, timestamps.Peek() + windowMilliseconds - nowMilliseconds);
                return new(AbuseDecision.RateLimited, timestamps.Count, limit, retryAfter);
            }

            timestamps.Enqueue(nowMilliseconds);
            return new(AbuseDecision.Allowed, timestamps.Count, limit, 0);
        }
    }

    public void Forget(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        lock (gate) events.Remove(key);
    }

    public int Cleanup(long nowMilliseconds, int maxIdleMilliseconds)
    {
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (maxIdleMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(maxIdleMilliseconds));

        lock (gate)
        {
            var threshold = nowMilliseconds - maxIdleMilliseconds;
            var stale = events.Where(pair => pair.Value.Count == 0 || pair.Value.Last() <= threshold)
                .Select(pair => pair.Key)
                .ToArray();
            foreach (var key in stale) events.Remove(key);
            return stale.Length;
        }
    }
}
