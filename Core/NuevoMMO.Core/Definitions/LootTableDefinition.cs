namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de una tabla de botín. Conserva la lista simple de ItemIds para compatibilidad,
/// pero Entries permite configurar probabilidad, cantidades y overrides de rolls como en un sistema MMO real.
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
        DefinitionId[]? itemIds,
        LootEntryDefinition[]? entries = null,
        Dictionary<string, float>? parameters = null,
        Dictionary<string, string>? metadata = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        var legacyItems = DefinitionModelGuards.CopyIds(itemIds, nameof(itemIds));
        Entries = entries?.ToArray() ?? [];
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        ItemIds = legacyItems
            .Concat(Entries.Select(static entry => entry.ItemId))
            .Distinct()
            .ToArray();
    }

    public DefinitionId[] ItemIds { get; }

    /// <summary>
    /// Entradas reales de generación de loot. ChancePercent usa rango 0..100,
    /// igual que el modelo de edición de drops tomado como referencia de Intersect/Broken_Reborn.
    /// </summary>
    public LootEntryDefinition[] Entries { get; }

    public Dictionary<string, float> Parameters { get; }
    public Dictionary<string, string> Metadata { get; }
}
