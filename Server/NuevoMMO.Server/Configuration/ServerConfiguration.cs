using System.Text.Json;

namespace NuevoMMO.Server.Configuration;

public sealed record DatabaseConfiguration
{
    public bool Enabled { get; init; } = false;
    public bool AutoMigrate { get; init; } = true;
    public string ConnectionString { get; init; } = "Host=127.0.0.1;Port=5432;Database=nuevommo;Username=nuevommo;Password=change-me";
}

public sealed record ServerConfiguration
{
    public string Environment { get; init; } = "Development";
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 7777;
    public int TickMilliseconds { get; init; } = 50;
    public int MaxPlayers { get; init; } = 32;
    public int MaxConnections { get; init; } = 32;
    public int MaxMessagesPerWindow { get; init; } = 100;
    public int HandshakeTimeoutSeconds { get; init; } = 5;
    public int ReadTimeoutSeconds { get; init; } = 10;
    public int WriteTimeoutSeconds { get; init; } = 3;
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
        if (Environment is not ("Development" or "Test")) throw new InvalidDataException("Environment debe ser Development o Test en el host actual.");
        if (string.IsNullOrWhiteSpace(Host)) throw new InvalidDataException("Host es requerido.");
        if (Port is < 1 or > 65535) throw new InvalidDataException("Port debe estar entre 1 y 65535.");
        if (TickMilliseconds is < 10 or > 1000) throw new InvalidDataException("TickMilliseconds debe estar entre 10 y 1000.");
        if (MaxPlayers is < 1 or > 10000) throw new InvalidDataException("MaxPlayers debe estar entre 1 y 10000.");
        if (MaxConnections is < 1 or > 10000) throw new InvalidDataException("MaxConnections debe estar entre 1 y 10000.");
        if (MaxMessagesPerWindow is < 1 or > 100000) throw new InvalidDataException("MaxMessagesPerWindow debe ser positivo.");
        if (HandshakeTimeoutSeconds is < 1 or > 300) throw new InvalidDataException("HandshakeTimeoutSeconds fuera de rango.");
        if (ReadTimeoutSeconds is < 1 or > 3600) throw new InvalidDataException("ReadTimeoutSeconds fuera de rango.");
        if (WriteTimeoutSeconds is < 1 or > 300) throw new InvalidDataException("WriteTimeoutSeconds fuera de rango.");
        if (AutosaveIntervalTicks is < 1 or > 1000000) throw new InvalidDataException("AutosaveIntervalTicks debe ser positivo.");
        if (Database.Enabled && string.IsNullOrWhiteSpace(Database.ConnectionString))
            throw new InvalidDataException("Database.ConnectionString es requerido cuando PostgreSQL está habilitado.");
    }

    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
