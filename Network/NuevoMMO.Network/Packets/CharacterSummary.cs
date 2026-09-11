using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record CharacterSummary(
    CharacterId Id,
    string Name,
    DefinitionId MapDefinition,
    Vector2Data Position,
    DefinitionId TraditionId);
