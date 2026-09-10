using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class MapValidator
{
    public IReadOnlyList<string> Validate(MapDefinition map)
    {
        var errors = new List<string>();
        if (!map.Bounds.IsValid) errors.Add("Bounds inválidos.");
        if (map.Bounds.Clamp(map.Spawn) != map.Spawn) errors.Add("Spawn fuera del mapa.");
        return errors;
    }
}
