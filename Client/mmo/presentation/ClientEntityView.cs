using Godot;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

public partial class ClientEntityView : Node2D
{
    public EntityId EntityId { get; set; }
    private ColorRect? marker;
    private Sprite2D? sprite;
    private Label? caption;

    public void InitializeMarker(Color color)
    {
        marker = new ColorRect { Size = new(14, 14), Position = new(-7, -7), Color = color };
        AddChild(marker);
        AddChild(CreateCaption(color));
    }

    public void InitializeTexture(Texture2D texture, Color captionColor)
    {
        sprite = new Sprite2D
        {
            Texture = texture,
            TextureFilter = TextureFilterEnum.Nearest,
            Position = new(0, -texture.GetHeight() / 2f)
        };
        AddChild(sprite);
        AddChild(CreateCaption(captionColor));
    }

    private Label CreateCaption(Color color)
    {
        caption = new Label
        {
            Position = new(-50, -28),
            Size = new(100, 18),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        caption.AddThemeFontSizeOverride("font_size", 11);
        caption.AddThemeColorOverride("font_color", color);
        return caption;
    }

    public void PresentMarker(EntityState entity, Vector2Data position)
    {
        EntityId = entity.Id;
        Position = new(position.X, position.Y);
        if (caption is not null)
            caption.Text = $"{KindGlyph(entity.Kind)} {entity.DisplayName}";
    }

    private static string KindGlyph(EntityKind kind) => kind switch
    {
        EntityKind.Resource => "R",
        EntityKind.WorldItem => "I",
        EntityKind.Projectile => "*",
        EntityKind.InteractiveObject => "E",
        EntityKind.Npc => "N",
        EntityKind.Mob => "M",
        _ => "?"
    };
}
