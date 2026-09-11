using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Configuration;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.NetworkHandlers;
using NuevoMMO.Server.Services;
using NuevoMMO.Server.Systems;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server;

public sealed class OscillatingMobPolicy : IMobMovementPolicy
{
    public Vector2Data NextVelocity(Mob mob, long tick, float seconds)
        => new(MathF.Sin(tick * seconds) > 0 ? 1 : -1, 0);
}

public sealed class ServerComposition
{
    public required WorldRuntime World { get; init; }
    public required GameSystems Systems { get; init; }
    public required PersistenceService Persistence { get; init; }
    public required PacketDispatcher<ServerPacketContext> Dispatcher { get; init; }
    public required DefinitionRegistry Definitions { get; init; }
    public required ISessionRepository Sessions { get; init; }
    public string PersistenceDescription { get; init; } = "InMemory";
}

public static class DevelopmentWorldFactory
{
    public static ServerComposition Create(string environment)
        => CreateInMemory(environment, ServerConfiguration.Development());

    public static ServerComposition Create(string environment, ServerConfiguration configuration)
        => CreateInMemory(environment, configuration);

    public static async Task<ServerComposition> CreateAsync(
        string environment,
        ServerConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        return configuration.Database.Provider switch
        {
            "memory" => CreateInMemory(environment, configuration),
            "sqlite" => await CreateSqliteAsync(environment, configuration, cancellationToken),
            "postgresql" => await CreatePostgresAsync(environment, configuration, cancellationToken),
            _ => throw new InvalidOperationException("Proveedor de persistencia no soportado.")
        };
    }

    private static async Task<ServerComposition> CreateSqliteAsync(
        string environment,
        ServerConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var paths = SqliteDatabasePaths.FromConfiguration(configuration.Database.Sqlite);
        if (configuration.Database.AutoMigrate)
            await SqliteMigrator.ApplyAsync(paths, cancellationToken);

        return CreateInternal(
            environment,
            configuration,
            new SqliteAccountRepository(paths.Auth),
            new SqliteSessionRepository(paths.Auth),
            new SqliteCharacterRepository(paths.Players),
            $"SQLite · auth={paths.Auth} · players={paths.Players} · game={paths.Game} · logs={paths.Logs}");
    }

    private static async Task<ServerComposition> CreatePostgresAsync(
        string environment,
        ServerConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var connectionString = configuration.Database.PostgreSqlConnectionString;
        if (configuration.Database.AutoMigrate)
            await PostgresMigrator.ApplyAsync(connectionString, cancellationToken);

        return CreateInternal(
            environment,
            configuration,
            new PostgresAccountRepository(connectionString),
            new PostgresSessionRepository(connectionString),
            new PostgresCharacterRepository(connectionString),
            "PostgreSQL");
    }

    private static ServerComposition CreateInMemory(string environment, ServerConfiguration configuration)
        => CreateInternal(environment, configuration,
            new InMemoryAccountRepository(), new InMemorySessionRepository(), new InMemoryCharacterRepository(), "InMemory");

    private static ServerComposition CreateInternal(
        string environment,
        ServerConfiguration configuration,
        IAccountRepository accounts,
        ISessionRepository sessions,
        ICharacterRepository characters,
        string persistenceDescription)
    {
        if (environment is not ("Development" or "Test")) throw new InvalidOperationException("Fixtures solo en Development/Test.");
        if (!string.Equals(environment, configuration.Environment, StringComparison.Ordinal))
            throw new InvalidOperationException("Environment no coincide con ServerConfiguration.");

        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "movement.json")));
        var data = document.RootElement;
        var map = new MapDefinition(
            new(data.GetProperty("map").GetGuid()), new("maps.development"), "Development", "Mapa técnico local.",
            true, 1, ["fixture"], new("maps.development.visual"),
            new(new(0, 0), new(data.GetProperty("width").GetSingle(), data.GetProperty("height").GetSingle())),
            new(data.GetProperty("spawnX").GetSingle(), data.GetProperty("spawnY").GetSingle()), new(32, 32));

        // Mob técnico matable: 250 HP / 100 XP. Son valores de fixture para probar el circuito, no balance de contenido.
        var mob = new MobDefinition(
            new(data.GetProperty("mob").GetGuid()), new("mobs.scout"), "Explorador XP", "Mob técnico matable para probar XP/subida de nivel.",
            true, 1, ["fixture", "development", "xp-test"], new("template.player"),
            behavior: new CreatureBehaviorDefinition(
                aggressive: false,
                movement: CreatureMovementMode.Stationary),
            combat: new CreatureCombatDefinition(
                level: 1,
                experience: 100,
                baseDamage: 0,
                maxVitals: new Dictionary<VitalId, float>
                {
                    [VitalId.Health] = 250,
                    [VitalId.Mana] = 0
                },
                parameters: new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                {
                    ["track_damage_telemetry"] = 1,
                    ["development_xp_target"] = 1
                }));

        var trainingDummyDefinition = TrainingDummyFixture.CreateDefinition();
        var package = ContentPackage.Empty("dev-1") with { Maps = [map], Mobs = [mob, trainingDummyDefinition] };
        var definitions = new GameDataLoader().Load(package);
        var systems = new GameSystems(definitions);
        var options = new WorldOptions(
            new(data.GetProperty("instance").GetInt64()), data.GetProperty("speed").GetSingle(),
            data.GetProperty("mobSpeed").GetSingle(), configuration.TickMilliseconds,
            data.GetProperty("interestRadius").GetSingle(), configuration.MaxPlayers);
        var world = new WorldRuntime(map, mob, options, new OscillatingMobPolicy(),
            new(data.GetProperty("mobX").GetSingle(), data.GetProperty("mobY").GetSingle()), systems);

        // EntityId reservado únicamente para el fixture Development/Test. El dummy sigue siendo un Mob normal.
        var dummyPosition = map.Bounds.Clamp(new Vector2Data(map.Spawn.X + 128f, map.Spawn.Y));
        world.AddEntity(TrainingDummyFixture.CreateEntity(new EntityId(9_000_000_000), world.Instance, dummyPosition));

        var persistence = new PersistenceService(characters, map);
        var auth = new AuthService(accounts, sessions, new PasswordHasher<string>());
        var characterService = new CharacterService(characters, map);
        var dispatcher = new PacketDispatcher<ServerPacketContext>(
            ServerHandlerRegistry.Create(
                world,
                auth,
                characterService,
                persistence,
                progression: systems.Progression,
                combat: systems.Combat),
            PacketDirection.ClientToServer);
        return new ServerComposition
        {
            World = world,
            Systems = systems,
            Persistence = persistence,
            Dispatcher = dispatcher,
            Definitions = definitions,
            Sessions = sessions,
            PersistenceDescription = persistenceDescription
        };
    }
}
