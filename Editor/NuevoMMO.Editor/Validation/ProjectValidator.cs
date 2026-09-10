using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class ProjectValidator(DefinitionRegistry registry)
{
    private readonly DefinitionValidator definitions = new();
    private readonly ReferenceValidator references = new(registry);
    private readonly MapValidator maps = new();

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        foreach (var definition in registry.GetAll<MapDefinition>()) errors.AddRange(maps.Validate(definition));
        foreach (var definition in registry.GetAll<MobDefinition>())
        {
            errors.AddRange(definitions.Validate(definition));
            errors.AddRange(references.Validate(definition));
        }
        return errors;
    }
}
