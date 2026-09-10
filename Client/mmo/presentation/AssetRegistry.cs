using Godot;

namespace NuevoMMO.GodotClient;

/// <summary>Template SpriteFrames extracted by tools/extract-template-frames.ps1, without gameplay nodes.</summary>
public sealed class AssetRegistry
{
    private SpriteFrames? playerFrames;
    public SpriteFrames PlayerFrames()
    {
        if (playerFrames is not null) return playerFrames;
        return playerFrames = GD.Load<SpriteFrames>("res://mmo/presentation/template_player_frames.tres")
            ?? throw new InvalidOperationException("SpriteFrames del template ausentes.");
    }
}
