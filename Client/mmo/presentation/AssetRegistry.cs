using Godot;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

/// <summary>Assets visuales del cliente resueltos por ContentKey; nunca forman parte de la autoridad del servidor.</summary>
public sealed class AssetRegistry
{
    private SpriteFrames? playerFrames;

    public SpriteFrames Frames(ContentKey visualKey)
    {
        if (visualKey == CanonicalCharacterAppearance.BaseVisual) return PlayerFrames();
        throw new KeyNotFoundException($"Visual no registrado en cliente: {visualKey}");
    }

    public SpriteFrames PlayerFrames()
    {
        if (playerFrames is not null) return playerFrames;
        return playerFrames = GD.Load<SpriteFrames>("res://mmo/presentation/template_player_frames.tres")
            ?? throw new InvalidOperationException("SpriteFrames del template ausentes.");
    }
}
