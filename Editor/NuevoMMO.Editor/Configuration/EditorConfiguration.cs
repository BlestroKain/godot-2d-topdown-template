namespace NuevoMMO.Editor;

public sealed class EditorConfiguration
{
    public EditorMode Mode { get; init; } = EditorMode.Offline;
    public string ContentPath { get; init; } = "gamedata";
}
