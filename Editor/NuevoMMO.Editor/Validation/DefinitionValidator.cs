using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class DefinitionValidator
{
    public IReadOnlyList<string> Validate(GameDefinition definition)
    {
        var errors = new List<string>();
        if (definition.Id.Value == Guid.Empty) errors.Add("ID vacío.");
        if (string.IsNullOrWhiteSpace(definition.Name)) errors.Add("Nombre vacío.");
        return errors;
    }
}
