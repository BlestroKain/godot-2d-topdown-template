namespace NuevoMMO.Server.Scheduling;

public sealed class TickScheduler(int tickMilliseconds)
{
    public int TickMilliseconds { get; } = tickMilliseconds is >= 10 and <= 1000
        ? tickMilliseconds : throw new ArgumentException("Tick inválido.");
}
