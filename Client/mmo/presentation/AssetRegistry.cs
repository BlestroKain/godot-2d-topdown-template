using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

/// <summary>
/// Lector Godot de la carpeta compartida <c>Client/resources</c> (<c>res://resources</c>).
/// Las Definitions solo traen ContentKey; el servidor no conoce estas rutas.
/// </summary>
public sealed class AssetRegistry
{
    private SpriteFrames? playerFrames;

    public static string PathFor(ContentKey visualKey) => ClientAssetPaths.Godot(visualKey);

    public SpriteFrames Frames(ContentKey visualKey)
    {
        if (visualKey == CanonicalCharacterAppearance.BaseVisual) return PlayerFrames();
        throw new KeyNotFoundException($"Visual no registrado en cliente: {visualKey}");
    }

    public bool TryTexture(ContentKey visualKey, out Texture2D? texture)
    {
        texture = null;
        if (visualKey.IsEmpty) return false;
        var path = PathFor(visualKey);
        if (!ResourceLoader.Exists(path)) return false;
        texture = GD.Load<Texture2D>(path);
        return texture is not null;
    }

    public SpriteFrames PlayerFrames()
    {
        if (playerFrames is not null) return playerFrames;
        return playerFrames = GD.Load<SpriteFrames>("res://mmo/presentation/template_player_frames.tres")
            ?? throw new InvalidOperationException("SpriteFrames del template ausentes.");
    }
}
