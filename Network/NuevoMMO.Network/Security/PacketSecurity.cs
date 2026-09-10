namespace NuevoMMO.Network;

public sealed class RateLimiter(int maxPerSecond)
{
    private long windowStart = Environment.TickCount64;
    private int count;
    public bool TryAdmit()
    {
        var now = Environment.TickCount64;
        if (now - windowStart >= 1000) { windowStart = now; count = 0; }
        if (count >= maxPerSecond) return false;
        count++;
        return true;
    }
}
