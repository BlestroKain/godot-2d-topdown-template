using NuevoMMO.Contracts;
using NuevoMMO.Domain;

namespace NuevoMMO.Simulation;

public sealed class MovementSystem
{
    public float Speed { get; }
    public int TickMilliseconds { get; }

    public MovementSystem(float speed, int tickMilliseconds)
    {
        if (!float.IsFinite(speed) || speed <= 0 || tickMilliseconds is < 10 or > 1000)
            throw new ArgumentException("Parámetros de movimiento inválidos.");
        Speed = speed; TickMilliseconds = tickMilliseconds;
    }

    // Exactly one input per server tick. Network packet arrival never advances simulation.
    public void Step(PlayerEntity player, MapDefinition map)
    {
        var before = player.Position;
        if (!player.Inputs.TryTake(out var input)) { player.ApplyMovement(before, default); return; }
        var length = MathF.Sqrt(input.X * input.X + input.Y * input.Y);
        var scale = length > 1 ? 1 / length : 1;
        var seconds = TickMilliseconds / 1000f;
        var after = map.Clamp(new(before.X + input.X * scale * Speed * seconds,
            before.Y + input.Y * scale * Speed * seconds));
        player.ApplyMovement(after, new((after.X - before.X) / seconds, (after.Y - before.Y) / seconds));
    }
}
