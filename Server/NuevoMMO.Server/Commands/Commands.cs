using NuevoMMO.Server.World;

namespace NuevoMMO.Server.Commands;

public interface ICommand
{
    string Name { get; }
    string Execute(string[] arguments);
}

public sealed class CommandParser
{
    public (string Name, string[] Arguments) Parse(string line)
    {
        var parts = (line ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? ("", []) : (parts[0].ToLowerInvariant(), parts.Skip(1).ToArray());
    }
}

public sealed class CommandRegistry
{
    private readonly Dictionary<string, ICommand> commands = new(StringComparer.OrdinalIgnoreCase);
    public void Register(ICommand command) => commands[command.Name] = command;
    public string Execute(string line)
    {
        var (name, arguments) = new CommandParser().Parse(line);
        return commands.TryGetValue(name, out var command) ? command.Execute(arguments) : "Comando desconocido.";
    }
}

public sealed class StatusCommand(WorldRuntime world) : ICommand
{
    public string Name => "status";
    public string Execute(string[] arguments) => $"tick={world.Tick} players={world.PlayerCount} entities={world.EntityCount}";
}

public sealed class PlayersCommand(WorldRuntime world) : ICommand
{
    public string Name => "players";
    public string Execute(string[] arguments) => string.Join(", ", world.OnlineNames());
}

public sealed class SaveCommand(Func<Task> save) : ICommand
{
    public string Name => "save";
    public string Execute(string[] arguments) { save().GetAwaiter().GetResult(); return "ok"; }
}

public sealed class ShutdownCommand(CancellationTokenSource stop) : ICommand
{
    public string Name => "shutdown";
    public string Execute(string[] arguments) { stop.Cancel(); return "shutting down"; }
}

public sealed class ReloadDefinitionsCommand(Action reload) : ICommand
{
    public string Name => "reload";
    public string Execute(string[] arguments) { reload(); return "definitions reloaded"; }
}
