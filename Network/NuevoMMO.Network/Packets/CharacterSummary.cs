using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record CharacterSummary(
    CharacterId Id,
    string Name,
    DefinitionId MapDefinition,
    Vector2Data Position,
    DefinitionId TraditionId)
{
    /// <summary>Compatibilidad con snapshots/fixtures anteriores a Tradición-en-creación.</summary>
    public CharacterSummary(CharacterId id, string name, DefinitionId mapDefinition, Vector2Data position)
        : this(id, name, mapDefinition, position, DefinitionId.Empty) { }
}
