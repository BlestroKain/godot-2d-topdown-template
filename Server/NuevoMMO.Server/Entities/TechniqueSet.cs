using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

/// <summary>
/// Estado runtime de técnicas aprendidas y cooldowns del personaje.
/// Los tiempos se expresan como milisegundos monotónicos del reloj del servidor.
/// </summary>
public sealed class TechniqueSet
{
    private readonly HashSet<DefinitionId> techniqueIds = [];
    private readonly Dictionary<DefinitionId, long> cooldowns = [];
    private readonly Dictionary<string, long> cooldownGroups = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<DefinitionId> TechniqueIds => techniqueIds;
    public IReadOnlyDictionary<DefinitionId, long> Cooldowns => cooldowns;
    public IReadOnlyDictionary<string, long> CooldownGroups => cooldownGroups;
    public long GlobalCooldownUntil { get; private set; }

    public bool Knows(DefinitionId techniqueId)
        => !techniqueId.IsEmpty && techniqueIds.Contains(techniqueId);

    public bool Learn(DefinitionId techniqueId)
    {
        if (techniqueId.IsEmpty) throw new ArgumentException("TechniqueId vacío.", nameof(techniqueId));
        return techniqueIds.Add(techniqueId);
    }

    public bool Forget(DefinitionId techniqueId)
    {
        if (techniqueId.IsEmpty) return false;
        cooldowns.Remove(techniqueId);
        return techniqueIds.Remove(techniqueId);
    }

    public void ReplaceKnown(IEnumerable<DefinitionId> techniques)
    {
        ArgumentNullException.ThrowIfNull(techniques);
        var incoming = techniques.ToArray();
        if (incoming.Any(static id => id.IsEmpty))
            throw new ArgumentException("La colección contiene TechniqueId vacío.", nameof(techniques));
        if (incoming.Distinct().Count() != incoming.Length)
            throw new ArgumentException("La colección contiene TechniqueId duplicado.", nameof(techniques));

        techniqueIds.Clear();
        foreach (var technique in incoming) techniqueIds.Add(technique);
        foreach (var id in cooldowns.Keys.Where(id => !techniqueIds.Contains(id)).ToArray()) cooldowns.Remove(id);
    }

    public bool IsReady(TechniqueDefinition definition, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!Knows(definition.Id)) return false;
        if (!definition.Timing.IgnoreGlobalCooldown && GlobalCooldownUntil > nowMilliseconds) return false;
        if (cooldowns.GetValueOrDefault(definition.Id) > nowMilliseconds) return false;
        return string.IsNullOrWhiteSpace(definition.Timing.CooldownGroup) ||
               cooldownGroups.GetValueOrDefault(definition.Timing.CooldownGroup) <= nowMilliseconds;
    }

    public long RemainingCooldown(TechniqueDefinition definition, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var readyAt = cooldowns.GetValueOrDefault(definition.Id);
        if (!definition.Timing.IgnoreGlobalCooldown) readyAt = Math.Max(readyAt, GlobalCooldownUntil);
        if (!string.IsNullOrWhiteSpace(definition.Timing.CooldownGroup))
            readyAt = Math.Max(readyAt, cooldownGroups.GetValueOrDefault(definition.Timing.CooldownGroup));
        return Math.Max(0, readyAt - nowMilliseconds);
    }

    public void StartCooldown(
        TechniqueDefinition definition,
        long nowMilliseconds,
        int effectiveCooldownMilliseconds,
        int globalCooldownMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!Knows(definition.Id)) throw new InvalidOperationException("La técnica no está aprendida.");
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (effectiveCooldownMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(effectiveCooldownMilliseconds));
        if (globalCooldownMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(globalCooldownMilliseconds));

        if (effectiveCooldownMilliseconds > 0)
        {
            var readyAt = checked(nowMilliseconds + effectiveCooldownMilliseconds);
            cooldowns[definition.Id] = readyAt;
            if (!string.IsNullOrWhiteSpace(definition.Timing.CooldownGroup))
                cooldownGroups[definition.Timing.CooldownGroup] = Math.Max(
                    cooldownGroups.GetValueOrDefault(definition.Timing.CooldownGroup), readyAt);
        }

        if (!definition.Timing.IgnoreGlobalCooldown && globalCooldownMilliseconds > 0)
            GlobalCooldownUntil = Math.Max(GlobalCooldownUntil, checked(nowMilliseconds + globalCooldownMilliseconds));
    }

    public void ClearExpiredCooldowns(long nowMilliseconds)
    {
        foreach (var id in cooldowns.Where(pair => pair.Value <= nowMilliseconds).Select(static pair => pair.Key).ToArray())
            cooldowns.Remove(id);
        foreach (var key in cooldownGroups.Where(pair => pair.Value <= nowMilliseconds).Select(static pair => pair.Key).ToArray())
            cooldownGroups.Remove(key);
        if (GlobalCooldownUntil <= nowMilliseconds) GlobalCooldownUntil = 0;
    }

    public void ClearCooldowns()
    {
        cooldowns.Clear();
        cooldownGroups.Clear();
        GlobalCooldownUntil = 0;
    }
}
