using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class EnvironmentEditor
{
    private MapDocument? document;

    public void Bind(MapDocument map) => document = map ?? throw new ArgumentNullException(nameof(map));

    public MapEnvironmentDefinition Get() => RequireDocument().Environment;

    public void Set(MapEnvironmentDefinition environment)
        => RequireDocument().Environment = environment ?? throw new ArgumentNullException(nameof(environment));

    private MapDocument RequireDocument() => document ?? throw new InvalidOperationException("No hay mapa abierto.");
}
