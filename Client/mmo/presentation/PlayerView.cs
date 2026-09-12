using Godot;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

/// <summary>
/// Replicated character presenter. Node hierarchy/layout live in player_view.tscn so visual work
/// stays editable in Godot; this script only binds runtime entity state to those nodes.
/// </summary>
public partial class PlayerView : Node2D
{
    private AnimatedSprite2D sprite = null!;
    private Label caption = null!;
    private string facing = "down";
    private bool nodesBound;

    public override void _Ready() => BindNodes();

    public void Initialize(SpriteFrames frames, bool local, EntityKind kind = EntityKind.Player)
    {
        BindNodes();
        sprite.SpriteFrames = frames;
        sprite.TextureFilter = TextureFilterEnum.Nearest;
        caption.AddThemeColorOverride("font_color", CaptionColor(kind, local));
        sprite.Modulate = kind switch
        {
            EntityKind.Mob => new Color("e38b8b"),
            EntityKind.Npc => new Color("8be3a4"),
            _ => Colors.White
        };
        sprite.Play("idle-down");
    }

    public void Present(EntityState entity, Vector2Data position, Vector2 motion, bool local)
    {
        BindNodes();
        Position = new(position.X, position.Y);
        if (motion.LengthSquared() > .001f)
            facing = MathF.Abs(motion.X) > MathF.Abs(motion.Y)
                ? motion.X < 0 ? "left" : "right"
                : motion.Y < 0 ? "up" : "down";
        var animation = (motion.LengthSquared() > .001f ? "walk-" : "idle-") + facing;
        if (sprite.Animation != animation) sprite.Play(animation);
        var suffix = local ? " · tú" : entity.Kind is EntityKind.Player ? "" : $" · {entity.Kind}";
        caption.Text = entity.DisplayName + suffix;
    }

    private void BindNodes()
    {
        if (nodesBound) return;
        sprite = GetNode<AnimatedSprite2D>("Sprite");
        caption = GetNode<Label>("Caption");
        nodesBound = true;
    }

    private static Color CaptionColor(EntityKind kind, bool local) => kind switch
    {
        EntityKind.Player when local => new Color("f4d887"),
        EntityKind.Player => new Color("9bddea"),
        EntityKind.Mob => new Color("ef8a8a"),
        EntityKind.Npc => new Color("8fe3a8"),
        _ => new Color("d7c15a")
    };
}
