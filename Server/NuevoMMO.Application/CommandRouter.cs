using NuevoMMO.Contracts;

namespace NuevoMMO.Application;

public interface ICommandHandler<in T> where T : IMessage
{
    void Handle(PlayerSession session, T command);
}

public sealed class MovementCommandHandler : ICommandHandler<MoveCommand>
{
    public void Handle(PlayerSession session, MoveCommand command)
    {
        if (session.State != SessionState.InWorld) throw new InvalidOperationException("Sesión fuera del mundo.");
        session.Player.Inputs.Enqueue(new(command.Number, command.X, command.Y));
    }
}

public sealed class CommandRouter
{
    private readonly Dictionary<Type, Action<PlayerSession, IMessage>> handlers = [];
    public CommandRouter() => Register(new MovementCommandHandler());
    private void Register<T>(ICommandHandler<T> handler) where T : IMessage
        => handlers.Add(typeof(T), (session, command) => handler.Handle(session, (T)command));
    public void Dispatch(PlayerSession session, IMessage command)
    {
        if (!handlers.TryGetValue(command.GetType(), out var handler)) throw new ArgumentException("Comando no permitido.");
        handler(session, command);
    }
}
