namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de una especie/variante hostil o combatible.
/// La entidad Mob runtime mantiene estado; esta Definition describe cómo debe nacer y comportarse.
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
        DefinitionId? lootTableId = null,
        LootMode lootMode = LootMode.Shared,
        CreatureBehaviorDefinition? behavior = null,
        CreatureCombatDefinition? combat = null,
        Dictionary<string, DefinitionId>? eventHooks = null,
        Dictionary<string, string>? metadata = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));
        if (lootTableId is { } loot && loot.IsEmpty)
            throw new ArgumentException("LootTableId no puede estar vacío cuando se especifica.", nameof(lootTableId));

        VisualKey = visualKey;
        LootTableId = lootTableId;
        LootMode = lootMode;
        Behavior = behavior ?? new CreatureBehaviorDefinition();
        Combat = combat ?? new CreatureCombatDefinition();
        EventHooks = DefinitionModelGuards.CopyDefinitionHooks(eventHooks, nameof(eventHooks));
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public ContentKey VisualKey { get; }
    public DefinitionId? LootTableId { get; }
    public LootMode LootMode { get; }

    /// <summary>
    /// Aggro, movimiento, huida, selección de objetivo, radio de visión/reset y combate entre NPCs.
    /// </summary>
    public CreatureBehaviorDefinition Behavior { get; }

    /// <summary>
    /// Nivel, experiencia, daño base, elemento, críticos, stats, vitales, scaling, técnicas e inmunidades.
    /// </summary>
    public CreatureCombatDefinition Combat { get; }

    /// <summary>
    /// Hooks como onDeath, onSpawn, onAggro, onReset, etc. El sistema de eventos resuelve las referencias.
    /// </summary>
    public Dictionary<string, DefinitionId> EventHooks { get; }
    public Dictionary<string, string> Metadata { get; }
}
