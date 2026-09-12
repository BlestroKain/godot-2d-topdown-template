using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class EditorConfiguration
{
    public EditorMode Mode { get; init; } = EditorMode.Offline;
    public string ContentPath { get; init; } = "Data/game.db";

    /// <summary>
    /// Única carpeta de assets. Editor, cliente C# y Godot leen aquí.
    /// Godot la ve como <c>res://resources</c> porque el proyecto está en <c>Client/</c>.
    /// </summary>
    public string ResourcesRoot { get; init; } = AssetCatalog.SharedRootFromRepo;
}
