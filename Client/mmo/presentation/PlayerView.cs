using Godot;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

public partial class PlayerView : Node2D
{
    private AnimatedSprite2D sprite = null!;
    private Label caption = null!;
    private string facing = "down";

    public void Initialize(SpriteFrames frames, bool local, EntityKind kind = EntityKind.Player)
    {
        sprite = new() { SpriteFrames = frames, Position = new(0, -16), TextureFilter = TextureFilterEnum.Nearest };
        caption = new() { Position = new(-70, -58), Size = new(140, 24), HorizontalAlignment = HorizontalAlignment.Center };
        caption.AddThemeFontSizeOverride("font_size", 12);
        caption.AddThemeColorOverride("font_color", CaptionColor(kind, local));
        if (kind == EntityKind.Mob) sprite.Modulate = new Color("e38b8b");
        else if (kind == EntityKind.Npc) sprite.Modulate = new Color("8be3a4");
        AddChild(sprite); AddChild(caption); sprite.Play("idle-down");
    }

    public void Present(EntityState entity, Vector2Data position, Vector2 motion, bool local)
    {
        Position = new(position.X, position.Y);
        if (motion.LengthSquared() > .001f)
            facing = MathF.Abs(motion.X) > MathF.Abs(motion.Y) ? (motion.X < 0 ? "left" : "right") : (motion.Y < 0 ? "up" : "down");
        var animation = (motion.LengthSquared() > .001f ? "walk-" : "idle-") + facing;
        if (sprite.Animation != animation) sprite.Play(animation);
        var suffix = local ? " · tú" : entity.Kind is EntityKind.Player ? "" : $" · {entity.Kind}";
        caption.Text = entity.DisplayName + suffix;
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
