using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Raíz de aplicación del Editor. No contiene UI Godot: expone los workspaces y servicios
/// que una interfaz visual puede enlazar tanto en modo offline como conectado.
/// </summary>
public sealed class EditorApplication
{
    public EditorConfiguration Configuration { get; }
    public DefinitionRegistry Definitions { get; } = new();
    public DirtyState Dirty { get; } = new();
    public EditorHistory History { get; } = new();
    public ProjectValidator Validator { get; }
    public MapEditor Maps { get; }
    public EventEditor Events { get; }

    public EditorApplication(EditorConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Validator = new(Definitions);
        Maps = new MapEditor(Definitions, History, Dirty);
        Events = new EventEditor(Definitions, History, Dirty);
    }
}
