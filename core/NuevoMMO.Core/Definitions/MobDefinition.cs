namespace NuevoMMO.Core;

/// <summary>
/// Describe un tipo de criatura. No representa una criatura viva del runtime.
/// </summary>
public sealed record MobDefinition : GameDefinition
{
    public MobDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey visualKey,
        DefinitionId? lootTableId = null)
        : base(
            id,
            key,
            name,
            description,
            enabled,
            version,
            tags)
    {
        if (visualKey.IsEmpty)
            throw new ArgumentException("VisualKey vacío.", nameof(visualKey));

        if (lootTableId is { } loot && loot.IsEmpty)
            throw new ArgumentException(
                "LootTableId no puede estar vacío cuando se especifica.",
                nameof(lootTableId));

        VisualKey = visualKey;
        LootTableId = lootTableId;
    }

    /// <summary>
    /// Clave que permite al cliente resolver la representación visual.
    /// El servidor no necesita conocer texturas, escenas ni recursos de Godot.
    /// </summary>
    public ContentKey VisualKey { get; }

    /// <summary>
    /// Referencia opcional a la tabla de botín. Se resuelve por DefinitionId.
    /// </summary>
    public DefinitionId? LootTableId { get; }
}
