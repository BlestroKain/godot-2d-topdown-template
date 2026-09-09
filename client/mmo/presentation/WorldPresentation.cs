using Godot;
using NuevoMMO.Client;
using NuevoMMO.Contracts;

namespace NuevoMMO.GodotClient;

public partial class WorldPresentation : Node2D
{
    private readonly Dictionary<EntityId, PlayerView> views = [];
    private readonly AssetRegistry assets = new();
    private MapProjection? map;
    private Camera2D camera = null!;

    public override void _Ready()
    {
        camera = new() { Position = new(320, 180), Enabled = true };
        AddChild(camera); YSortEnabled = true;
    }

    public void Present(ClientWorldState state, Vector2 input, float fraction)
    {
        if (state.Session is not { } session || state.Predictor is null) { Clear(); return; }
        if (map != session.Map) { map = session.Map; QueueRedraw(); }
        foreach (var id in views.Keys.Where(id => !state.Entities.ContainsKey(id)).ToArray()) { views[id].QueueFree(); views.Remove(id); }
        foreach (var entity in state.Entities.Values)
        {
            if (entity.VisualKey != "template.player") continue;
            var local = entity.Id == session.Self;
            if (!views.TryGetValue(entity.Id, out var view))
            {
                view = new(); view.Initialize(assets.PlayerFrames(), local); AddChild(view); views.Add(entity.Id, view);
            }
            var position = local ? state.Predictor.Preview(input.X, input.Y, fraction) : state.SampleRemote(entity.Id, NetworkBridge.Now);
            var motion = local ? input : new Vector2(entity.Velocity.X, entity.Velocity.Y);
            view.Present(entity, position, motion, local);
            if (local) camera.Position = new(position.X, position.Y);
        }
    }

    public override void _Draw()
    {
        if (map is null) return;
        DrawRect(new(0, 0, map.Width, map.Height), new Color("243c36"));
        for (float x = 0; x <= map.Width; x += 32) DrawLine(new(x, 0), new(x, map.Height), new Color("2b453e"));
        for (float y = 0; y <= map.Height; y += 32) DrawLine(new(0, y), new(map.Width, y), new Color("2b453e"));
        DrawRect(new(0, 0, map.Width, map.Height), new Color("b6a778"), false, 2);
    }

    private void Clear()
    {
        if (map is null && views.Count == 0) return;
        foreach (var view in views.Values) view.QueueFree();
        views.Clear(); map = null; QueueRedraw();
    }
}
