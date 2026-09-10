using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public readonly record struct MovementInput(long Sequence, long ClientTick, float X, float Y);

public sealed class MovementInputBuffer
{
    public const int Capacity = 32;
    private readonly Queue<MovementInput> pending = new();
    public long LastAccepted { get; private set; }
    public long LastProcessed { get; private set; }
    public int Count => pending.Count;
    public void Enqueue(InputFrame frame)
    {
        if (frame.Sequence <= 0 || frame.Sequence != LastAccepted + 1) throw new ArgumentException("Input repetido o fuera de orden.");
        if (!float.IsFinite(frame.X) || !float.IsFinite(frame.Y) || MathF.Abs(frame.X) > 1 || MathF.Abs(frame.Y) > 1)
            throw new ArgumentException("Dirección inválida.");
        if (pending.Count >= Capacity) throw new InvalidOperationException("Cola de input llena.");
        pending.Enqueue(new(frame.Sequence, frame.ClientTick, frame.X, frame.Y)); LastAccepted = frame.Sequence;
    }
    public bool TryTake(out MovementInput input)
    {
        if (!pending.TryDequeue(out input)) return false;
        LastProcessed = input.Sequence; return true;
    }
}

public sealed class MovementSystem(float speed, int tickMilliseconds)
{
    public float Speed { get; } = float.IsFinite(speed) && speed > 0 ? speed : throw new ArgumentException("Velocidad inválida.");
    public int TickMilliseconds { get; } = tickMilliseconds is >= 10 and <= 1000 ? tickMilliseconds : throw new ArgumentException("Tick inválido.");

    public void Step(Player player, MapDefinition map)
    {
        var before = player.Position;
        if (!player.Inputs.TryTake(out var input)) { player.ApplyMovement(before, default); return; }
        var length = MathF.Sqrt(input.X * input.X + input.Y * input.Y); var scale = length > 1 ? 1 / length : 1;
        var seconds = TickMilliseconds / 1000f;
        var desired = map.Bounds.Clamp(new(before.X + input.X * scale * Speed * seconds, before.Y + input.Y * scale * Speed * seconds));
        var after = IsBlocked(map, desired) ? before : desired;
        player.ApplyMovement(after, new((after.X - before.X) / seconds, (after.Y - before.Y) / seconds));
    }

    public static bool IsBlocked(MapDefinition map, Vector2Data position)
    {
        // MapDefinition no es el sistema autoritativo de colisión. Bounds ya recorta el desplazamiento.
        _ = map;
        return !position.IsFinite;
    }
}

public interface IMobMovementPolicy { Vector2Data NextVelocity(Mob mob, long tick, float seconds); }

public sealed class MobMovementSystem(float speed, int tickMilliseconds, IMobMovementPolicy policy)
{
    public void Step(Mob mob, MapDefinition map, long tick)
    {
        var seconds = tickMilliseconds / 1000f; var direction = policy.NextVelocity(mob, tick, seconds);
        var length = MathF.Sqrt(direction.LengthSquared); var scale = length > 1 ? 1 / length : 1;
        var desired = map.Bounds.Clamp(new(mob.Position.X + direction.X * scale * speed * seconds,
            mob.Position.Y + direction.Y * scale * speed * seconds));
        var after = MovementSystem.IsBlocked(map, desired) ? mob.Position : desired;
        mob.MoveTo(after, new((after.X - mob.Position.X) / seconds, (after.Y - mob.Position.Y) / seconds));
    }
}
