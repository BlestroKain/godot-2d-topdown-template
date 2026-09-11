using NuevoMMO.Core;

namespace NuevoMMO.Server.Systems;

public sealed record DamageMeterSnapshot(
    float LastRawDamage,
    int LastAppliedDamage,
    Element LastElement,
    bool LastCritical,
    float Dps5Seconds,
    float Dps10Seconds,
    long TotalDamage,
    int Hits,
    int CriticalHits);

/// <summary>
/// Medidor genérico de daño para desarrollo/admin. No cambia la resolución de
/// combate y puede usarse con cualquier entidad marcada para telemetría.
/// </summary>
public sealed class CombatTelemetry
{
    private readonly Dictionary<EntityId, DamageMeter> meters = [];

    public void Record(EntityId target, DamageResult result, long nowMilliseconds)
    {
        if (target.Value <= 0) throw new ArgumentException("Target inválido.", nameof(target));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (!meters.TryGetValue(target, out var meter))
            meters[target] = meter = new DamageMeter();
        meter.Record(result, nowMilliseconds);
    }

    public DamageMeterSnapshot? Snapshot(EntityId target, long nowMilliseconds)
        => meters.TryGetValue(target, out var meter) ? meter.Snapshot(nowMilliseconds) : null;

    public void Reset(EntityId target) => meters.Remove(target);

    private sealed class DamageMeter
    {
        private readonly Queue<HitSample> recent = new();
        private long totalDamage;
        private int hits;
        private int criticalHits;
        private DamageResult? last;

        public void Record(DamageResult result, long now)
        {
            last = result;
            totalDamage = checked(totalDamage + result.AppliedDamage);
            hits++;
            if (result.Critical) criticalHits++;
            recent.Enqueue(new HitSample(now, result.AppliedDamage));
            Trim(now, 10_000);
        }

        public DamageMeterSnapshot Snapshot(long now)
        {
            Trim(now, 10_000);
            var dps5 = SumSince(now - 5_000) / 5f;
            var dps10 = SumSince(now - 10_000) / 10f;
            return new DamageMeterSnapshot(
                last?.RawDamage ?? 0,
                last?.AppliedDamage ?? 0,
                last?.Element ?? Element.Neutral,
                last?.Critical ?? false,
                dps5,
                dps10,
                totalDamage,
                hits,
                criticalHits);
        }

        private int SumSince(long minimum) => recent.Where(hit => hit.Timestamp >= minimum).Sum(hit => hit.Damage);

        private void Trim(long now, long window)
        {
            var minimum = now - window;
            while (recent.TryPeek(out var hit) && hit.Timestamp < minimum)
                recent.Dequeue();
        }

        private readonly record struct HitSample(long Timestamp, int Damage);
    }
}
