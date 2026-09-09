using Godot;
using NuevoMMO.Contracts;

namespace NuevoMMO.GodotClient;

public partial class PlayerView : Node2D
{
    private AnimatedSprite2D sprite = null!;
    private Label caption = null!;
    private string facing = "down";

    public void Initialize(SpriteFrames frames, bool local)
    {
        sprite = new() { SpriteFrames = frames, Position = new(0, -16), TextureFilter = TextureFilterEnum.Nearest };
        caption = new() { Position = new(-70, -58), Size = new(140, 24), HorizontalAlignment = HorizontalAlignment.Center };
        caption.AddThemeFontSizeOverride("font_size", 12);
        caption.AddThemeColorOverride("font_color", local ? new Color("f4d887") : new Color("9bddea"));
        AddChild(sprite); AddChild(caption); sprite.Play("idle-down");
    }

    public void Present(EntityProjection entity, WorldPosition position, Vector2 motion, bool local)
    {
        Position = new(position.X, position.Y);
        if (motion.LengthSquared() > .001f)
            facing = MathF.Abs(motion.X) > MathF.Abs(motion.Y) ? (motion.X < 0 ? "left" : "right") : (motion.Y < 0 ? "up" : "down");
        var animation = (motion.LengthSquared() > .001f ? "walk-" : "idle-") + facing;
        if (sprite.Animation != animation) sprite.Play(animation);
        caption.Text = entity.Name + (local ? " · tú" : "");
    }
}
