using NuevoMMO.Contracts;

namespace NuevoMMO.Client;

public sealed class MovementPredictor(MapProjection map, WorldPosition initial)
{
    private readonly Queue<MoveCommand> pending = new();
    private long nextInput;
    private long lastAck;
    public WorldPosition Position { get; private set; } = initial;
    public int PendingCount => pending.Count;
    public long Corrections { get; private set; }

    public MoveCommand Predict(float x, float y)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || MathF.Abs(x) > 1 || MathF.Abs(y) > 1)
            throw new ArgumentException("Input inválido.");
        if (pending.Count >= 32) throw new InvalidOperationException("Servidor sin confirmar inputs.");
        var input = new MoveCommand(++nextInput, x, y);
        pending.Enqueue(input); Position = Integrate(Position, input, 1);
        return input;
    }

    public void Reconcile(MovementCorrection correction)
    {
        if (correction.LastProcessedInput < lastAck || correction.LastProcessedInput > nextInput || !correction.Position.IsFinite)
            throw new InvalidDataException("Acknowledgement inválido.");
        lastAck = correction.LastProcessedInput;
        while (pending.TryPeek(out var first) && first.Number <= lastAck) pending.Dequeue();
        var before = Position;
        Position = correction.Position;
        foreach (var input in pending) Position = Integrate(Position, input, 1);
        if (MathF.Abs(before.X - Position.X) + MathF.Abs(before.Y - Position.Y) > .01f) Corrections++;
    }

    // Rendering a fractional future step is presentation only and never sent as a result.
    public WorldPosition Preview(float x, float y, float fraction) => Integrate(Position, new(0, x, y), Math.Clamp(fraction, 0, 1));

    private WorldPosition Integrate(WorldPosition position, MoveCommand input, float fraction)
    {
        var length = MathF.Sqrt(input.X * input.X + input.Y * input.Y);
        var distance = map.MovementSpeed * map.TickMilliseconds / 1000f * fraction / MathF.Max(1, length);
        return new(Math.Clamp(position.X + input.X * distance, 0, map.Width), Math.Clamp(position.Y + input.Y * distance, 0, map.Height));
    }
}
