namespace NuevoMMO.Core;

/// <summary>
/// Describe un tipo de NPC. No representa una instancia viva del runtime ni contiene lógica de interacción.
/// </summary>
public sealed record NpcDefinition : GameDefinition
{
    public NpcDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey visualKey)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty)
            throw new ArgumentException("VisualKey vacío.", nameof(visualKey));

        VisualKey = visualKey;
    }

    /// <summary>
    /// Clave que permite al cliente resolver la representación visual del NPC.
    /// El servidor no conoce texturas, escenas ni recursos de Godot.
    /// </summary>
    public ContentKey VisualKey { get; }
}
