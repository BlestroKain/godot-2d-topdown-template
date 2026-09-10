using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class ProfessionSet
{
    public HashSet<DefinitionId> ProfessionIds { get; } = [];
}
