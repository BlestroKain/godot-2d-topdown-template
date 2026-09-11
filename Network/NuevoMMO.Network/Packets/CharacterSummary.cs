using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record CharacterSummary(
    CharacterId Id,
    string Name,
    DefinitionId MapDefinition,
    Vector2Data Position,
    DefinitionId TraditionId,
    CharacterAppearance Appearance)
{
    /// <summary>Compatibilidad con snapshots/fixtures anteriores a Tradición/apariencia persistentes.</summary>
    public CharacterSummary(CharacterId id, string name, DefinitionId mapDefinition, Vector2Data position)
        : this(id, name, mapDefinition, position, DefinitionId.Empty, CanonicalCharacterAppearance.Default) { }

    public CharacterSummary(CharacterId id, string name, DefinitionId mapDefinition, Vector2Data position, DefinitionId traditionId)
        : this(id, name, mapDefinition, position, traditionId, CanonicalCharacterAppearance.Default) { }
}
