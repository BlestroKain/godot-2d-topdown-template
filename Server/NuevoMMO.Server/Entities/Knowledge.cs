using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class Knowledge
{
    public HashSet<DefinitionId> KnownTechniques { get; } = [];
}
