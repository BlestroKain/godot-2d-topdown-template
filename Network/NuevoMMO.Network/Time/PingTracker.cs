namespace NuevoMMO.Network;

public sealed class PingTracker
{
    private readonly Queue<double> samples = [];
    public double LastMilliseconds { get; private set; }
    public double AverageMilliseconds { get; private set; }

    public void Record(double roundTripMilliseconds)
    {
        if (!double.IsFinite(roundTripMilliseconds) || roundTripMilliseconds < 0)
            throw new ArgumentException("Muestra de ping inválida.");
        LastMilliseconds = roundTripMilliseconds;
        samples.Enqueue(roundTripMilliseconds);
        while (samples.Count > 16) samples.Dequeue();
        AverageMilliseconds = samples.Average();
    }
}
