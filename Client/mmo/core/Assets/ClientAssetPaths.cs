using NuevoMMO.Core;

namespace NuevoMMO.Client;

/// <summary>
/// Rutas de assets del cliente C#. Misma carpeta que el editor y Godot:
/// <c>Client/resources</c> en disco, <c>res://resources</c> en el proyecto Godot.
/// No abre archivos: el cliente Godot usa <c>AssetRegistry</c>.
/// </summary>
public static class ClientAssetPaths
{
    public const string SharedRootFromRepo = AssetCatalog.SharedRootFromRepo;
    public const string GodotRoot = "res://" + AssetCatalog.DefaultRoot;

    public static string Godot(ContentKey key) => AssetCatalog.GodotPath(key);

    public static string Disk(string sharedRoot, ContentKey key) => AssetCatalog.DiskPath(sharedRoot, key);

    public static string Relative(ContentKey key) => AssetCatalog.RelativePath(key);
}
