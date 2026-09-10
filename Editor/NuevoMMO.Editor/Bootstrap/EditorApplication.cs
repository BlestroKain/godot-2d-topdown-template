using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class EditorApplication
{
    public EditorConfiguration Configuration { get; }
    public DefinitionRegistry Definitions { get; } = new();
    public DirtyState Dirty { get; } = new();
    public EditorHistory History { get; } = new();
    public ProjectValidator Validator { get; }

    public EditorApplication(EditorConfiguration configuration)
    {
        Configuration = configuration;
        Validator = new(Definitions);
    }
}
