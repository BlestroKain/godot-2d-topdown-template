namespace NuevoMMO.Core;

/// <summary>
/// Describe un conjunto de ítems candidatos a botín.
/// Las reglas definitivas de probabilidad, cantidad y generación permanecen en diseño.
/// </summary>
public sealed record LootTableDefinition : GameDefinition
{
    public LootTableDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        DefinitionId[]? itemIds)
        : base(id, key, name, description, enabled, version, tags)
    {
        var items = itemIds ?? [];
        if (items.Any(static item => item.IsEmpty))
            throw new ArgumentException("ItemIds contiene un DefinitionId vacío.", nameof(itemIds));
        if (items.Distinct().Count() != items.Length)
            throw new ArgumentException("ItemIds contiene referencias duplicadas.", nameof(itemIds));

        ItemIds = [.. items];
    }

    /// <summary>
    /// Ítems candidatos. Cada referencia debe resolver a un ItemDefinition.
    /// </summary>
    public DefinitionId[] ItemIds { get; }
}
