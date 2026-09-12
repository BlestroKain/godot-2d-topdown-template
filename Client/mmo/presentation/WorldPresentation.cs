using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

public partial class WorldPresentation : Node2D
{
    private const string PlayerViewScenePath = "res://mmo/presentation/player_view.tscn";
    private const string EntityViewScenePath = "res://mmo/presentation/client_entity_view.tscn";

    private readonly Dictionary<EntityId, Node2D> views = [];
    private readonly AssetRegistry assets = new();
    private MapProjection? map;
    private Camera2D camera = null!;
    private PackedScene playerViewScene = null!;
    private PackedScene entityViewScene = null!;

    public Camera2D Camera => camera;

    public override void _Ready()
    {
        playerViewScene = GD.Load<PackedScene>(PlayerViewScenePath)
            ?? throw new InvalidOperationException($"No se pudo cargar {PlayerViewScenePath}.");
        entityViewScene = GD.Load<PackedScene>(EntityViewScenePath)
            ?? throw new InvalidOperationException($"No se pudo cargar {EntityViewScenePath}.");

        camera = GetNodeOrNull<Camera2D>("Camera");
        if (camera is null)
        {
            camera = new() { Position = new(320, 180), Enabled = true };
            AddChild(camera);
        }

        YSortEnabled = true;
    }

    public void Present(ClientWorldState state, Vector2 input, float fraction)
    {
        if (state.Session.Map is not { } sessionMap || state.Predictor is null)
        {
            Clear();
            return;
        }

        if (map is null || map.Instance != sessionMap.Instance)
        {
            map = sessionMap;
            ConfigureCameraForMap(sessionMap);
            QueueRedraw();
        }

        foreach (var id in views.Keys.Where(id => !state.Entities.All.ContainsKey(id)).ToArray())
        {
            views[id].QueueFree();
            views.Remove(id);
        }

        foreach (var entity in state.Entities.All.Values)
        {
            var local = entity.Id == state.Session.Self;
            var position = local
                ? state.Predictor.Preview(input.X, input.Y, fraction)
                : state.SampleRemote(entity.Id, NetworkBridge.Now);
            var motion = local ? input : new Vector2(entity.Velocity.X, entity.Velocity.Y);
            if (!views.TryGetValue(entity.Id, out var view))
            {
                view = SpawnView(entity, local);
                views.Add(entity.Id, view);
            }

            switch (view)
            {
                case PlayerView player:
                    player.Present(entity, position, motion, local);
                    break;
                case ClientEntityView marker:
                    marker.PresentMarker(entity, position);
                    break;
            }

            if (local)
                camera.Position = new(position.X, position.Y);
        }
    }

    public override void _Draw()
    {
        if (map is null) return;
        DrawRect(new(0, 0, map.Bounds.Width, map.Bounds.Height), new Color("243c36"));
        for (float x = 0; x <= map.Bounds.Width; x += 32)
            DrawLine(new(x, 0), new(x, map.Bounds.Height), new Color("2b453e"));
        for (float y = 0; y <= map.Bounds.Height; y += 32)
            DrawLine(new(0, y), new(map.Bounds.Width, y), new Color("2b453e"));
        DrawRect(new(0, 0, map.Bounds.Width, map.Bounds.Height), new Color("b6a778"), false, 2);
    }

    private Node2D SpawnView(EntityState entity, bool local)
    {
        if (entity.Kind is EntityKind.Player or EntityKind.Mob or EntityKind.Npc)
        {
            var player = playerViewScene.Instantiate<PlayerView>();
            AddChild(player);
            player.Initialize(assets.PlayerFrames(), local, entity.Kind);
            return player;
        }

        var marker = entityViewScene.Instantiate<ClientEntityView>();
        AddChild(marker);
        if (assets.TryTexture(entity.VisualKey, out var texture) && texture is not null)
            marker.InitializeTexture(texture, MarkerColor(entity.Kind));
        else
            marker.InitializeMarker(MarkerColor(entity.Kind));
        return marker;
    }

    private void ConfigureCameraForMap(MapProjection projection)
    {
        // Camera bounds are presentation-only; authoritative movement remains on the server.
        camera.LimitLeft = 0;
        camera.LimitTop = 0;
        camera.LimitRight = Math.Max(1, (int)MathF.Ceiling(projection.Bounds.Width));
        camera.LimitBottom = Math.Max(1, (int)MathF.Ceiling(projection.Bounds.Height));
    }

    private static Color MarkerColor(EntityKind kind) => kind switch
    {
        EntityKind.Resource => new Color("d7c15a"),
        EntityKind.WorldItem => new Color("e08a3c"),
        EntityKind.Projectile => new Color("f2f2f2"),
        EntityKind.InteractiveObject => new Color("7ec8e3"),
        _ => new Color("c94c4c")
    };

    private void Clear()
    {
        if (map is null && views.Count == 0) return;
        foreach (var view in views.Values) view.QueueFree();
        views.Clear();
        map = null;
        QueueRedraw();
    }
}
