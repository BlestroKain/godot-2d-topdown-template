using Godot;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

/// <summary>
/// Presenter for non-character world entities. The node tree lives in client_entity_view.tscn;
/// runtime code only selects the visual mode and binds replicated state.
/// </summary>
public partial class ClientEntityView : Node2D
{
    public EntityId EntityId { get; set; }

    private ColorRect marker = null!;
    private Sprite2D sprite = null!;
    private Label caption = null!;
    private bool nodesBound;

    public override void _Ready() => BindNodes();

    public void InitializeMarker(Color color)
    {
        BindNodes();
        marker.Visible = true;
        marker.Color = color;
        sprite.Visible = false;
        caption.AddThemeColorOverride("font_color", color);
    }

    public void InitializeTexture(Texture2D texture, Color captionColor)
    {
        BindNodes();
        marker.Visible = false;
        sprite.Visible = true;
        sprite.Texture = texture;
        sprite.TextureFilter = TextureFilterEnum.Nearest;
        sprite.Position = new(0, -texture.GetHeight() / 2f);
        caption.AddThemeColorOverride("font_color", captionColor);
    }

    public void PresentMarker(EntityState entity, Vector2Data position)
    {
        BindNodes();
        EntityId = entity.Id;
        Position = new(position.X, position.Y);
        caption.Text = $"{KindGlyph(entity.Kind)} {entity.DisplayName}";
    }

    private void BindNodes()
    {
        if (nodesBound) return;
        marker = GetNode<ColorRect>("Marker");
        sprite = GetNode<Sprite2D>("Sprite");
        caption = GetNode<Label>("Caption");
        nodesBound = true;
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
