using NuevoMMO.Server;
using NuevoMMO.Server.Configuration;

var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

if (args.Length == 2 && args[0] == "--config")
    configPath = Path.GetFullPath(args[1]);
else if (args.Length != 0)
{
    Console.Error.WriteLine("Uso: NuevoMMO.Server [--config <ruta-config.json>]");
    return 2;
}

ServerConfiguration configuration;
try
{
    configuration = ServerConfiguration.Load(configPath);
}
catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
{
    Console.Error.WriteLine($"No se pudo iniciar el servidor: {exception.Message}");
    return 2;
}

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stop.Cancel();
};

ServerComposition composition;
try
{
    composition = await DevelopmentWorldFactory.CreateAsync(configuration.Environment, configuration, stop.Token);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"No se pudo inicializar persistencia/mundo: {exception.Message}");
    return 3;
}

Console.WriteLine($"NuevoMMO Server · {configuration.Environment}");
Console.WriteLine($"Config: {configPath}");
Console.WriteLine(configuration.Database.Enabled ? "Persistencia: PostgreSQL" : "Persistencia: InMemory");

await new ServerHost(
    composition.World,
    composition.Persistence,
    composition.Dispatcher,
    configuration,
    composition.Sessions).RunAsync(stop.Token);

return 0;
