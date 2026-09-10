namespace NuevoMMO.Server.Security;

public sealed class AbuseDetector
{
    public bool IsBurst(int packets, int limit) => packets > limit;
}
