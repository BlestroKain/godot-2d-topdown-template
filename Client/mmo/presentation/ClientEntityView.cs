using Godot;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

public partial class ClientEntityView : Node2D
{
    public EntityId EntityId { get; set; }
    private ColorRect? marker;
    private Label? caption;

    public void InitializeMarker(Color color)
    {
        marker = new ColorRect { Size = new(14, 14), Position = new(-7, -7), Color = color };
        caption = new Label
        {
            Position = new(-50, -28),
            Size = new(100, 18),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        caption.AddThemeFontSizeOverride("font_size", 11);
        caption.AddThemeColorOverride("font_color", color);
        AddChild(marker);
        AddChild(caption);
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
