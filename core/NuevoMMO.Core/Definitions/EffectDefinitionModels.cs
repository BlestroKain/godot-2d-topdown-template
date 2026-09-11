namespace NuevoMMO.Core;

public enum EffectDisposition : byte
{
    Neutral,
    Beneficial,
    Harmful
}

public enum EffectStackPolicy : byte
{
    RefreshDuration,
    AddStack,
    Replace,
    Independent,
    IgnoreIfPresent
}

public sealed record EffectLifecycleDefinition
{
    public EffectLifecycleDefinition(
        int durationMilliseconds = 0,
        int tickIntervalMilliseconds = 0,
        EffectStackPolicy stackPolicy = EffectStackPolicy.RefreshDuration,
        int maxStacks = 1,
        bool dispellable = true,
        Dictionary<string, float>? parameters = null)
    {
        if (durationMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
        if (tickIntervalMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(tickIntervalMilliseconds));
        if (durationMilliseconds > 0 && tickIntervalMilliseconds > durationMilliseconds)
            throw new ArgumentException("TickInterval no puede ser mayor que Duration.", nameof(tickIntervalMilliseconds));
        if (maxStacks < 1) throw new ArgumentOutOfRangeException(nameof(maxStacks));

        DurationMilliseconds = durationMilliseconds;
        TickIntervalMilliseconds = tickIntervalMilliseconds;
        StackPolicy = stackPolicy;
        MaxStacks = maxStacks;
        Dispellable = dispellable;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public int DurationMilliseconds { get; }
    public int TickIntervalMilliseconds { get; }
    public EffectStackPolicy StackPolicy { get; }
    public int MaxStacks { get; }
    public bool Dispellable { get; }
    public Dictionary<string, float> Parameters { get; }
}
