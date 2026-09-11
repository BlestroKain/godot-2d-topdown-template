using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class StatusInstance : IClientStatus
{
    public StatusInstance(DefinitionId effectId, long remainingMilliseconds, long totalMilliseconds)
    {
        if (effectId.IsEmpty) throw new ArgumentException("EffectId vacío.", nameof(effectId));
        if (remainingMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(remainingMilliseconds));
        if (totalMilliseconds < remainingMilliseconds) throw new ArgumentOutOfRangeException(nameof(totalMilliseconds));
        EffectId = effectId;
        RemainingMilliseconds = remainingMilliseconds;
        TotalMilliseconds = totalMilliseconds;
    }

    public DefinitionId EffectId { get; }
    public long RemainingMilliseconds { get; private set; }
    public long TotalMilliseconds { get; }
    public bool IsActive => RemainingMilliseconds > 0;

    public void Tick(long deltaMilliseconds)
    {
        if (deltaMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaMilliseconds));
        RemainingMilliseconds = Math.Max(0, RemainingMilliseconds - deltaMilliseconds);
    }
}
