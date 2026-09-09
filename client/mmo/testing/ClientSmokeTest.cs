#if DEBUG
using System.Text.Json;
using Godot;
using NuevoMMO.Contracts;
using NuevoMMO.GodotClient;

namespace NuevoMMO.Testing;

/// <summary>Explicit test scene; not loaded by the game. Inputs still pass through the real protocol.</summary>
public partial class ClientSmokeTest : Node
{
    private MmoGame game = null!;
    private double start;
    private double duration = 8;
    private float direction = 1;
    private string reportPath = "";
    private string? screenshotPath;
    private bool captured;
    private bool ended;
    private int snapshots;
    private int maxVisible;
    private int despawns;
    private float maxDistance;
    private WorldPosition? initial;
    private readonly Dictionary<EntityId, WorldPosition> firstRemote = [];
    private float remoteDistance;

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        string? Arg(string prefix) => args.FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];
        reportPath = Arg("--report=") ?? throw new InvalidOperationException("--report requerido.");
        screenshotPath = Arg("--screenshot=");
        if (Arg("--seconds=") is { } seconds) duration = double.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture);
        if (Arg("--direction=") is { } value) direction = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var port = int.Parse(Arg("--port=") ?? "7777", System.Globalization.CultureInfo.InvariantCulture);
        game = new(); AddChild(game);
        game.Network.WorldUpdated += () => snapshots++;
        game.Network.ConnectToServer("127.0.0.1", port, Arg("--name=") ?? "Prueba");
        start = NetworkBridge.Now;
    }

    public override void _Process(double delta)
    {
        if (ended) return;
        var elapsed = NetworkBridge.Now - start;
        game.TestInput = elapsed is > 2 and < 3 ? new Vector2(direction, 0) : Vector2.Zero;
        var state = game.Network.World;
        if (state.Session is { } session && state.Predictor is { } predictor)
        {
            initial ??= predictor.Position;
            maxDistance = Math.Max(maxDistance, Distance(initial.Value, predictor.Position));
            maxVisible = Math.Max(maxVisible, state.Entities.Count);
            foreach (var entity in state.Entities.Values.Where(entity => entity.Id != session.Self))
            {
                firstRemote.TryAdd(entity.Id, entity.Position);
                remoteDistance = Math.Max(remoteDistance, Distance(firstRemote[entity.Id], entity.Position));
            }
            despawns = Math.Max(despawns, firstRemote.Keys.Count(id => !state.Entities.ContainsKey(id)));
        }
        if (screenshotPath is not null && !captured && elapsed > 4) { captured = true; Capture(); }
        if (elapsed < duration) return;
        ended = true;
        var passed = game.Network.InWorld && snapshots > 20 && maxVisible >= 2 && maxDistance > 20 && remoteDistance > 20;
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new { passed, snapshots, maxVisible, maxDistance, remoteDistance,
            despawns, entityId = state.Session?.Self.Value, pending = state.Predictor?.PendingCount, status = game.Network.Status }));
        game.Network.DisconnectFromServer(); GetTree().Quit(passed ? 0 : 1);
    }

    private async void Capture()
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = GetViewport().GetTexture().GetImage();
        image.SavePng(screenshotPath!);
    }

    private static float Distance(WorldPosition a, WorldPosition b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
#endif
