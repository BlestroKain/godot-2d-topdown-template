namespace NuevoMMO.Core;

/// <summary>
/// Define candidatos y reglas de aparición. La colocación concreta pertenece al mapa;
/// esta Definition controla qué puede aparecer, cantidades, grupos y respawn.
/// </summary>
public sealed record SpawnTableDefinition : GameDefinition
{
    public SpawnTableDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        DefinitionId[]? mobIds,
        SpawnEntryDefinition[]? entries = null,
        SpawnSelectionMode selectionMode = SpawnSelectionMode.WeightedRandom,
        int maximumTotalAlive = 0,
        Dictionary<string, float>? parameters = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        var legacyMobs = DefinitionModelGuards.CopyIds(mobIds, nameof(mobIds));
        var normalizedEntries = entries?.ToList() ?? [];

        foreach (var mobId in legacyMobs)
        {
            if (!normalizedEntries.Any(entry => entry.Kind == SpawnEntityKind.Mob && entry.DefinitionId == mobId))
                normalizedEntries.Add(new SpawnEntryDefinition(SpawnEntityKind.Mob, mobId));
        }

        if (maximumTotalAlive < 0) throw new ArgumentOutOfRangeException(nameof(maximumTotalAlive));
        if (selectionMode == SpawnSelectionMode.WeightedRandom && normalizedEntries.Count > 0 &&
            normalizedEntries.All(static entry => entry.Weight <= 0))
            throw new ArgumentException("WeightedRandom requiere al menos una entrada con Weight > 0.", nameof(entries));

        Entries = normalizedEntries.ToArray();
        MobIds = Entries
            .Where(static entry => entry.Kind == SpawnEntityKind.Mob)
            .Select(static entry => entry.DefinitionId)
            .Distinct()
            .ToArray();
        SelectionMode = selectionMode;
        MaximumTotalAlive = maximumTotalAlive;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    /// <summary>Vista de compatibilidad de mobs presentes en la tabla.</summary>
    public DefinitionId[] MobIds { get; }
    public SpawnEntryDefinition[] Entries { get; }
    public SpawnSelectionMode SelectionMode { get; }
    public int MaximumTotalAlive { get; }
    public Dictionary<string, float> Parameters { get; }
}
