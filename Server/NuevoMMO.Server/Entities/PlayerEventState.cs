namespace NuevoMMO.Server.Entities;

/// <summary>
/// Switches y variables de evento del personaje. Runtime; persistencia completa es P1.
/// </summary>
public sealed class PlayerEventState
{
    public Dictionary<string, bool> Switches { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, float> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<Guid> TouchingEvents { get; } = [];
    public Dictionary<Guid, long> LastTimerFire { get; } = [];
    public HashSet<string> ConsumedTriggers { get; } = new(StringComparer.Ordinal);

    public bool GetSwitch(string key) => !string.IsNullOrWhiteSpace(key) && Switches.GetValueOrDefault(key.Trim());

    public void SetSwitch(string key, bool value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Switch key vacía.", nameof(key));
        Switches[key.Trim()] = value;
    }

    public float GetVariable(string key) => string.IsNullOrWhiteSpace(key) ? 0 : Variables.GetValueOrDefault(key.Trim());

    public void SetVariable(string key, float value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Variable key vacía.", nameof(key));
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        Variables[key.Trim()] = value;
    }
}
