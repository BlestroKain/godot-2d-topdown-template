using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

public sealed class MapLoader
{
    public static string ScenePath(ContentKey visualKey) => $"res://scenes/levels/{visualKey.Value}.tscn";
}
