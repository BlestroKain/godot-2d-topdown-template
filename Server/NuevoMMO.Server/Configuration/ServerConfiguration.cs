using System.Text.Json;

namespace NuevoMMO.Server.Configuration;

public sealed record SqliteDatabaseFiles
{
    public string Auth { get; init; } = "Data/auth.db";
    public string Players { get; init; } = "Data/players.db";
    public string Game { get; init; } = "Data/game.db";
    public string Logs { get; init; } = "Data/logs.db";
}

public sealed record DatabaseConfiguration
{
    public string Provider { get; init; } = "sqlite";
    public bool AutoMigrate { get; init; } = true;
    public SqliteDatabaseFiles Sqlite { get; init; } = new();
    public string PostgreSqlConnectionString { get; init; } =
        "Host=127.0.0.1;Port=5432;Database=nuevommo;Username=nuevommo;Password=change-me";
}

public sealed record ServerConfiguration
{
    public string Environment { get; init; } = "Development";
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 7777;
    public int TickMilliseconds { get; init; } = 50;
    public long InstanceId { get; init; } = 1;
    public float MovementSpeed { get; init; } = 120;
    public float MobSpeed { get; init; } = 40;
    public float InterestRadius { get; init; } = 60;
    public int MaxPlayers { get; init; } = 32;
    public int MaxConnections { get; init; } = 32;
    public int MaxMessagesPerWindow { get; init; } = 100;
    public int HandshakeTimeoutSeconds { get; init; } = 5;
    /// <summary>Timeout de handshake/lectura inicial. No aplica a la sesión en mundo.</summary>
    public int ReadTimeoutSeconds { get; init; } = 10;
    public int WriteTimeoutSeconds { get; init; } = 3;
    /// <summary>Intervalo de keepalive (ServerTime) hacia clientes conectados.</summary>
    public int KeepAliveIntervalSeconds { get; init; } = 5;
    /// <summary>Cierra la sesión si el cliente no envía ningún paquete durante este tiempo.</summary>
    public int IdleTimeoutSeconds { get; init; } = 120;
    public int AutosaveIntervalTicks { get; init; } = 40;
    public DatabaseConfiguration Database { get; init; } = new();

    public static ServerConfiguration Development(int port = 7777) => new() { Port = port };

    public static ServerConfiguration Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path)) throw new FileNotFoundException($"No se encontró la configuración del servidor: {path}", path);
        try
        {
            var json = File.ReadAllText(path);
            var configuration = JsonSerializer.Deserialize<ServerConfiguration>(json, JsonOptions)
                ?? throw new InvalidDataException("El archivo de configuración está vacío.");
            configuration.Validate();
            return configuration;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"JSON inválido en '{path}'.", exception);
        }
    }

    public void Validate()
    {
        if (Environment is not ("Development" or "Test" or "Production"))
            throw new InvalidDataException("Environment debe ser Development, Test o Production.");
        if (string.IsNullOrWhiteSpace(Host)) throw new InvalidDataException("Host es requerido.");
        if (Port is < 0 or > 65535) throw new InvalidDataException("Port debe estar entre 0 y 65535. 0 selecciona un puerto efímero.");
        if (TickMilliseconds is < 10 or > 1000) throw new InvalidDataException("TickMilliseconds debe estar entre 10 y 1000.");
        if (InstanceId < 1) throw new InvalidDataException("InstanceId debe ser positivo.");
        if (!float.IsFinite(MovementSpeed) || MovementSpeed is < 1 or > 2000)
            throw new InvalidDataException("MovementSpeed fuera de rango.");
        if (!float.IsFinite(MobSpeed) || MobSpeed is < 0 or > 2000)
            throw new InvalidDataException("MobSpeed fuera de rango.");
        if (!float.IsFinite(InterestRadius) || InterestRadius is < 16 or > 10_000)
            throw new InvalidDataException("InterestRadius fuera de rango.");
        if (MaxPlayers is < 1 or > 10000) throw new InvalidDataException("MaxPlayers debe estar entre 1 y 10000.");
        if (MaxConnections is < 1 or > 10000) throw new InvalidDataException("MaxConnections debe estar entre 1 y 10000.");
        if (MaxMessagesPerWindow is < 1 or > 100000) throw new InvalidDataException("MaxMessagesPerWindow debe ser positivo.");
        if (HandshakeTimeoutSeconds is < 1 or > 300) throw new InvalidDataException("HandshakeTimeoutSeconds fuera de rango.");
        if (ReadTimeoutSeconds is < 1 or > 3600) throw new InvalidDataException("ReadTimeoutSeconds fuera de rango.");
        if (WriteTimeoutSeconds is < 1 or > 300) throw new InvalidDataException("WriteTimeoutSeconds fuera de rango.");
        if (KeepAliveIntervalSeconds is < 1 or > 120) throw new InvalidDataException("KeepAliveIntervalSeconds fuera de rango.");
        if (IdleTimeoutSeconds is < 15 or > 3600) throw new InvalidDataException("IdleTimeoutSeconds debe estar entre 15 y 3600.");
        if (AutosaveIntervalTicks is < 1 or > 1000000) throw new InvalidDataException("AutosaveIntervalTicks debe ser positivo.");

        if (Database.Provider is not ("sqlite" or "postgresql" or "memory"))
            throw new InvalidDataException("Database.Provider debe ser sqlite, postgresql o memory.");

        if (Database.Provider == "sqlite")
        {
            foreach (var (name, value) in new[]
            {
                ("Auth", Database.Sqlite.Auth), ("Players", Database.Sqlite.Players),
                ("Game", Database.Sqlite.Game), ("Logs", Database.Sqlite.Logs)
            })
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidDataException($"Database.Sqlite.{name} es requerido.");
            }
        }

        if (Database.Provider == "postgresql" && string.IsNullOrWhiteSpace(Database.PostgreSqlConnectionString))
            throw new InvalidDataException("Database.PostgreSqlConnectionString es requerido.");
    }

    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
