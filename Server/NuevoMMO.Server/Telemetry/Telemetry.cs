using System.Text.Json;

namespace NuevoMMO.Server.Telemetry;

public sealed class ServerMetrics
{
    public double TickTime { get; private set; }
    public double Tps { get; private set; }
    public int PlayersOnline { get; private set; }
    public long PacketsIn { get; private set; }
    public long PacketsOut { get; private set; }
    public double DbLatency { get; private set; }
    public int EntityCount { get; private set; }
    public int MapInstanceCount { get; private set; } = 1;
    public long Memory { get; private set; }
    public long InvalidPackets { get; private set; }

    public void ObserveTick(double tickMilliseconds, int players, int entities, int maps)
    {
        TickTime = tickMilliseconds;
        Tps = tickMilliseconds <= 0 ? 0 : 1000d / Math.Max(tickMilliseconds, 1);
        PlayersOnline = players;
        EntityCount = entities;
        MapInstanceCount = maps;
        Memory = GC.GetTotalMemory(false);
    }

    public void PacketIn() => PacketsIn++;
    public void PacketOut() => PacketsOut++;
    public void Invalid() => InvalidPackets++;
    public void ObserveDb(double milliseconds) => DbLatency = milliseconds;
}

/// <summary>
/// Logger estructurado y seguro para el servidor. Los campos sensibles se redactan por nombre y
/// los valores de texto se normalizan para impedir inyección de líneas en los logs.
/// </summary>
public static class ServerLog
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password", "passwd", "token", "secret", "credential", "authorization", "cookie"
    ];

    public static void Info(string message)
        => Write("info", "server", "message", ("message", message));

    public static void Info(string area, string action, params (string Key, object? Value)[] fields)
        => Write("info", area, action, fields);

    public static void Warn(string area, string action, params (string Key, object? Value)[] fields)
        => Write("warn", area, action, fields);

    public static void Error(string area, string action, Exception exception, params (string Key, object? Value)[] fields)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write("error", area, action,
            [.. fields, ("exception", exception.GetType().Name), ("error", exception.Message)]);
    }

    public static string Format(string level, string area, string action, params (string Key, object? Value)[] fields)
    {
        var record = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["level"] = Clean(level),
            ["area"] = Clean(area),
            ["action"] = Clean(action)
        };

        foreach (var (key, value) in fields)
        {
            var cleanKey = Clean(key);
            if (string.IsNullOrWhiteSpace(cleanKey) || record.ContainsKey(cleanKey)) continue;
            record[cleanKey] = IsSensitive(cleanKey) ? "[REDACTED]" : CleanValue(value);
        }

        return JsonSerializer.Serialize(record);
    }

    private static void Write(string level, string area, string action, params (string Key, object? Value)[] fields)
        => Console.WriteLine(Format(level, area, action, fields));

    private static object? CleanValue(object? value)
        => value switch
        {
            null => null,
            string text => Clean(text),
            Guid guid => guid.ToString("N"),
            Enum enumValue => enumValue.ToString(),
            _ => value
        };

    private static bool IsSensitive(string key)
        => SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static string Clean(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        Span<char> buffer = value.Length <= 1024 ? stackalloc char[value.Length] : new char[value.Length];
        var length = 0;
        foreach (var character in value)
        {
            if (character is '\r' or '\n' or '\t')
            {
                if (length == 0 || buffer[length - 1] != ' ') buffer[length++] = ' ';
                continue;
            }
            if (!char.IsControl(character)) buffer[length++] = character;
        }
        return new string(buffer[..length]);
    }
}
