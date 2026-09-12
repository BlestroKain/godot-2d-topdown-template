using NuevoMMO.Core;

namespace NuevoMMO.Client;

/// <summary>
/// Adaptador de rutas para el cliente Godot. La semántica de assets vive en
/// <see cref="AssetCatalog"/>; el cliente solo expone las mismas rutas en forma
/// conveniente para res:// y para pruebas contra disco.
/// </summary>
public static class ClientAssetPaths
{
    public const string SharedRootFromRepo = AssetCatalog.SharedRootFromRepo;
    public const string GodotRoot = "res://resources";

    public static string Godot(ContentKey key) => AssetCatalog.GodotPath(key);

    public static string Disk(string sharedRoot, ContentKey key) => AssetCatalog.DiskPath(sharedRoot, key);
}
