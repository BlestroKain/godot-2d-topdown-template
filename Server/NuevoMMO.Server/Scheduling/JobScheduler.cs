namespace NuevoMMO.Server.Scheduling;

public sealed class JobScheduler
{
    private readonly Queue<Action> jobs = [];
    public void Enqueue(Action job) => jobs.Enqueue(job);
    public void RunPending()
    {
        while (jobs.Count > 0) jobs.Dequeue()();
    }
}
