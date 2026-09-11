using Godot;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

/// <summary>
/// Preview reutilizable del personaje. Renderiza los mismos SpriteFrames registrados para gameplay
/// dentro de un SubViewport; no mantiene una representación visual paralela del personaje.
/// </summary>
public partial class CharacterPreview : SubViewportContainer
{
    private readonly AssetRegistry assets = new();
    private SubViewport viewport = null!;
    private AnimatedSprite2D sprite = null!;
    private bool ready;
    private CharacterAppearance pendingAppearance = CanonicalCharacterAppearance.Default;

    public override void _Ready()
    {
        Stretch = true;
        MouseFilter = MouseFilterEnum.Ignore;

        viewport = new SubViewport
        {
            Name = "PreviewViewport",
            Size = new Vector2I(256, 256),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        AddChild(viewport);

        var stage = new Node2D { Name = "Stage" };
        viewport.AddChild(stage);
        sprite = new AnimatedSprite2D
        {
            Name = "Character",
            Position = new Vector2(128, 150),
            Scale = new Vector2(3, 3),
            TextureFilter = TextureFilterEnum.Nearest
        };
        stage.AddChild(sprite);
        ready = true;
        Present(pendingAppearance);
    }

    public void Present(CharacterAppearance appearance)
    {
        pendingAppearance = appearance ?? CanonicalCharacterAppearance.Default;
        if (!ready) return;
        sprite.SpriteFrames = assets.Frames(pendingAppearance.BaseVisual);
        sprite.Play("idle-down");
    }
}
