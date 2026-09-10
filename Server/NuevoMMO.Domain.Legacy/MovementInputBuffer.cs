namespace NuevoMMO.Domain;

public readonly record struct MovementInput(long Number, float X, float Y);

// Adapted from GodotMMO's own queue, with contiguous numbering and a bounded backlog.
public sealed class MovementInputBuffer
{
    public const int Capacity = 32; // Technical limit, not balance.
    private readonly Queue<MovementInput> pending = new();
    public long LastAccepted { get; private set; }
    public long LastProcessed { get; private set; }
    public int Count => pending.Count;

    public void Enqueue(MovementInput input)
    {
        if (input.Number <= 0 || input.Number != LastAccepted + 1)
            throw new ArgumentException("Input repetido o fuera de orden.");
        if (!float.IsFinite(input.X) || !float.IsFinite(input.Y) || MathF.Abs(input.X) > 1 || MathF.Abs(input.Y) > 1)
            throw new ArgumentException("Dirección inválida.");
        if (pending.Count >= Capacity) throw new InvalidOperationException("Cola de input llena.");
        pending.Enqueue(input); LastAccepted = input.Number;
    }

    public bool TryTake(out MovementInput input)
    {
        if (!pending.TryDequeue(out input)) return false;
        LastProcessed = input.Number;
        return true;
    }
}
