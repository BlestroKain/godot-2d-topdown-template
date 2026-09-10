using NuevoMMO.Server;

if (args.Length == 0 || args[0] is not ("--development" or "--test"))
{
    Console.Error.WriteLine("Este host solo tiene composición Development/Test. Usar --development [--port 7777].");
    return 2;
}
var port = 7777;
if (args.Length == 3 && args[1] == "--port" && int.TryParse(args[2], out var parsed) && parsed is > 0 and <= 65535) port = parsed;
else if (args.Length != 1) { Console.Error.WriteLine("Argumentos inválidos."); return 2; }
var composition = DevelopmentWorldFactory.Create(args[0] == "--test" ? "Test" : "Development");
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
await new ServerHost(composition.World, composition.Persistence, composition.Dispatcher, port).RunAsync(stop.Token);
return 0;
