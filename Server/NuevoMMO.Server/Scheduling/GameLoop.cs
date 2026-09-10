namespace NuevoMMO.Server.Scheduling;

public sealed class GameLoop(TickScheduler ticks)
{
    public TickScheduler Ticks { get; } = ticks;
}
