namespace NuevoMMO.Server.Admin;

public sealed class AdminAudit
{
    private readonly List<string> entries = [];
    public IReadOnlyList<string> Entries => entries;
    public void Record(string actor, string action) => entries.Add($"{DateTimeOffset.UtcNow:o} {actor} {action}");
}
