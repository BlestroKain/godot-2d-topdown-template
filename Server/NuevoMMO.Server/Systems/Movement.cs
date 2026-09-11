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
    public void Reset() { pending.Clear(); LastAccepted = 0; LastProcessed = 0; }
}

public sealed class MovementSystem(float speed, int tickMilliseconds)
{
    public float Speed { get; } = float.IsFinite(speed) && speed > 0 ? speed : throw new ArgumentException("Velocidad inválida.");
    public int TickMilliseconds { get; } = tickMilliseconds is >= 10 and <= 1000 ? tickMilliseconds : throw new ArgumentException("Tick inválido.");

    public void Step(Player player, MapDefinition map, Func<Vector2Data, bool>? additionalBlockedAt = null)
    {
        var before = player.Position;
        if (!player.Inputs.TryTake(out var input)) { player.ApplyMovement(before, default); return; }
        var length = MathF.Sqrt(input.X * input.X + input.Y * input.Y); var scale = length > 1 ? 1 / length : 1;
        var seconds = TickMilliseconds / 1000f;
        var delta = new Vector2Data(input.X * scale * Speed * seconds, input.Y * scale * Speed * seconds);
        var movementCollider = player.CollisionProfile.MovementCollider;
        var motion = MotionSolver2D.Resolve(before, delta, map.Bounds, movementCollider,
            position => IsBlocked(map, position, movementCollider) || (additionalBlockedAt?.Invoke(position) ?? false));
        var after = motion.Final;
        player.ApplyMovement(after, new((after.X - before.X) / seconds, (after.Y - before.Y) / seconds));
    }

    public static bool IsBlocked(MapDefinition map, Vector2Data position, CollisionShape? movementCollider = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        return !position.IsFinite || MapCollisionRuntime.For(map).BlocksMovement(movementCollider, position);
    }
}

public interface IMobMovementPolicy { Vector2Data NextVelocity(Mob mob, long tick, float seconds); }

public sealed class StationaryAwareMobPolicy(IMobMovementPolicy moving) : IMobMovementPolicy
{
    public Vector2Data NextVelocity(Mob mob, long tick, float seconds)
        => mob.Behavior.Movement == CreatureMovementMode.Stationary ? default : moving.NextVelocity(mob, tick, seconds);
}

public sealed class MobMovementSystem(float speed, int tickMilliseconds, IMobMovementPolicy policy)
{
    public void Step(Mob mob, MapDefinition map, long tick, Func<Vector2Data, bool>? additionalBlockedAt = null)
    {
        var seconds = tickMilliseconds / 1000f;
        var direction = mob.CombatState.InCombat ? default : policy.NextVelocity(mob, tick, seconds);
        ApplyDirection(mob, map, direction, speed, tickMilliseconds, additionalBlockedAt);
    }

    public void StepDirection(Mob mob, MapDefinition map, Vector2Data direction, Func<Vector2Data, bool>? additionalBlockedAt = null)
        => ApplyDirection(mob, map, direction, speed, tickMilliseconds, additionalBlockedAt);

    public static void ApplyDirection(
        Mob mob,
        MapDefinition map,
        Vector2Data direction,
        float movementSpeed,
        int deltaMilliseconds,
        Func<Vector2Data, bool>? additionalBlockedAt = null)
    {
        ArgumentNullException.ThrowIfNull(mob); ArgumentNullException.ThrowIfNull(map);
        if (!direction.IsFinite) throw new ArgumentException("Dirección de mob no finita.", nameof(direction));
        if (!float.IsFinite(movementSpeed) || movementSpeed < 0) throw new ArgumentOutOfRangeException(nameof(movementSpeed));
        if (deltaMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(deltaMilliseconds));

        var seconds = deltaMilliseconds / 1000f;
        var length = MathF.Sqrt(direction.LengthSquared); var scale = length > 1 ? 1 / length : 1;
        var before = mob.Position;
        var delta = new Vector2Data(direction.X * scale * movementSpeed * seconds, direction.Y * scale * movementSpeed * seconds);
        var movementCollider = mob.CollisionProfile.MovementCollider;
        var motion = MotionSolver2D.Resolve(before, delta, map.Bounds, movementCollider,
            position => MovementSystem.IsBlocked(map, position, movementCollider) || (additionalBlockedAt?.Invoke(position) ?? false));
        var after = motion.Final;
        mob.MoveTo(after, new((after.X - before.X) / seconds, (after.Y - before.Y) / seconds));
    }
}
