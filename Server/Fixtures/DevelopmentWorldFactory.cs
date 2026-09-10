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
    public required PersistenceService Persistence { get; init; }
    public required PacketDispatcher<ServerPacketContext> Dispatcher { get; init; }
    public required DefinitionRegistry Definitions { get; init; }
}

public static class DevelopmentWorldFactory
{
    public static ServerComposition Create(string environment)
        => Create(environment, ServerConfiguration.Development());

    public static ServerComposition Create(string environment, ServerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (environment is not ("Development" or "Test")) throw new InvalidOperationException("Fixtures solo en Development/Test.");
        if (!string.Equals(environment, configuration.Environment, StringComparison.Ordinal))
            throw new InvalidOperationException("Environment no coincide con ServerConfiguration.");

        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "movement.json")));
        var data = document.RootElement;
        var map = new MapDefinition(
            new(data.GetProperty("map").GetGuid()),
            new("maps.development"),
            "Development",
            "Mapa técnico local.",
            true,
            1,
            ["fixture"],
            new("maps.development.visual"),
            new(new(0, 0), new(data.GetProperty("width").GetSingle(), data.GetProperty("height").GetSingle())),
            new(data.GetProperty("spawnX").GetSingle(), data.GetProperty("spawnY").GetSingle()),
            new(32, 32));
        var mob = new MobDefinition(
            new(data.GetProperty("mob").GetGuid()),
            new("mobs.scout"),
            "Explorador",
            "Mob de fixture.",
            true,
            1,
            ["fixture"],
            new("template.player"));
        var package = ContentPackage.Empty("dev-1") with { Maps = [map], Mobs = [mob] };
        var definitions = new GameDataLoader().Load(package);
        var options = new WorldOptions(
            new(data.GetProperty("instance").GetInt64()),
            data.GetProperty("speed").GetSingle(),
            data.GetProperty("mobSpeed").GetSingle(),
            configuration.TickMilliseconds,
            data.GetProperty("interestRadius").GetSingle(),
            configuration.MaxPlayers);
        var world = new WorldRuntime(map, mob, options, new OscillatingMobPolicy(),
            new(data.GetProperty("mobX").GetSingle(), data.GetProperty("mobY").GetSingle()));
        var accounts = new InMemoryAccountRepository();
        var characters = new InMemoryCharacterRepository();
        var persistence = new PersistenceService(characters, map);
        var auth = new AuthService(accounts, new PasswordHasher<string>());
        var characterService = new CharacterService(characters, map);
        var dispatcher = new PacketDispatcher<ServerPacketContext>(
            ServerHandlerRegistry.Create(world, auth, characterService, persistence), PacketDirection.ClientToServer);
        return new ServerComposition { World = world, Persistence = persistence, Dispatcher = dispatcher, Definitions = definitions };
    }
}
