using Godot;
using NuevoMMO.Client;

namespace NuevoMMO.GodotClient;

public partial class DebugOverlay : CanvasLayer
{
    private Label label = null!;
    public NetworkDebug Network { get; } = new();
    public EntityDebug Entities { get; } = new();
    public PerformanceDebug Performance { get; } = new();
    public PacketInspector Packets { get; } = new();
    public CollisionDebug Collision { get; } = new();

    public override void _Ready()
    {
        label = new() { Position = new(10, 360) };
        AddChild(label);
    }

    public void Present(ClientWorldState state)
    {
        Performance.Fps = Engine.GetFramesPerSecond();
        Entities.Count = state.Entities.All.Count;
        label.Text = $"FPS {Performance.Fps:0} · ping {Network.Ping:0}ms · tick {state.LastTick} · map {state.Session.Map?.Definition.Value} · ents {Entities.Count}";
    }
}
