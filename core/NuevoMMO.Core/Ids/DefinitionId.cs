namespace NuevoMMO.Core;

public readonly record struct DefinitionId(Guid Value)
{
    public static DefinitionId New() => new(Guid.NewGuid());
}
