using System.Text.Json;
using NuevoMMO.Application;

namespace NuevoMMO.Server;

// TEMPORARY FIXTURE. Only the Development/Test executable composition may load this.
public static class DevelopmentWorldFactory
{
    public static WorldRuntime Create(string environment)
    {
        if (environment is not ("Development" or "Test")) throw new InvalidOperationException("Fixtures solo en Development/Test.");
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "movement.json")));
        var data = document.RootElement;
        return new(new(new(data.GetProperty("map").GetGuid()), new(data.GetProperty("instance").GetInt64()),
            data.GetProperty("width").GetSingle(), data.GetProperty("height").GetSingle(),
            new(data.GetProperty("spawnX").GetSingle(), data.GetProperty("spawnY").GetSingle()),
            data.GetProperty("speed").GetSingle(), data.GetProperty("tickMilliseconds").GetInt32(),
            data.GetProperty("interestRadius").GetSingle(), data.GetProperty("maxPlayers").GetInt32()));
    }
}
