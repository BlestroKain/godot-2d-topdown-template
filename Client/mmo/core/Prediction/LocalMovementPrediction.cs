using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.Client;

public sealed class LocalMovementPrediction(MapProjection map, Vector2Data initial)
{
    private readonly Queue<InputFrame> pending = [];
    private long nextInput;
    private long lastAck;
    public Vector2Data Position { get; private set; } = initial;
    public int PendingCount => pending.Count;
    public long Corrections { get; private set; }

    public MoveRequest Predict(float x, float y)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || MathF.Abs(x) > 1 || MathF.Abs(y) > 1)
            throw new ArgumentException("Input inválido.");
        if (pending.Count >= 32) throw new InvalidOperationException("Servidor sin confirmar inputs.");
        var input = new InputFrame(++nextInput, nextInput, x, y);
        pending.Enqueue(input);
        Position = Integrate(Position, input, 1);
        return new MoveRequest(input);
    }

    public void Reconcile(MovementCorrection correction)
    {
        if (correction.LastProcessedInput < lastAck || correction.LastProcessedInput > nextInput || !correction.Position.IsFinite)
            throw new InvalidDataException("Acknowledgement inválido.");
        lastAck = correction.LastProcessedInput;
        while (pending.TryPeek(out var first) && first.Sequence <= lastAck) pending.Dequeue();
        var before = Position;
        Position = correction.Position;
        foreach (var input in pending) Position = Integrate(Position, input, 1);
        if (MathF.Abs(before.X - Position.X) + MathF.Abs(before.Y - Position.Y) > .01f) Corrections++;
    }

    public Vector2Data Preview(float x, float y, float fraction) => Integrate(Position, new(0, 0, x, y), Math.Clamp(fraction, 0, 1));

    private Vector2Data Integrate(Vector2Data position, InputFrame input, float fraction)
    {
        var length = MathF.Sqrt(input.X * input.X + input.Y * input.Y);
        var distance = map.MovementSpeed * map.TickMilliseconds / 1000f * fraction / MathF.Max(1, length);
        return map.Bounds.Clamp(new(position.X + input.X * distance, position.Y + input.Y * distance));
    }
}
