using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class TechniqueSet
{
    public HashSet<DefinitionId> TechniqueIds { get; } = [];
}
