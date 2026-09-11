namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de un NPC. Puede ser puramente interactivo o también combatible.
/// La instancia runtime mantiene posición/estado; toda la configuración editable vive aquí.
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
        ContentKey visualKey,
        bool combatEnabled = false,
        CreatureBehaviorDefinition? behavior = null,
        CreatureCombatDefinition? combat = null,
        DefinitionId? lootTableId = null,
        LootMode lootMode = LootMode.Shared,
        Dictionary<string, DefinitionId>? services = null,
        Dictionary<string, DefinitionId>? eventHooks = null,
        Dictionary<string, string>? metadata = null,
        EntityCollisionProfileDefinition? collision = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));
        if (lootTableId is { } loot && loot.IsEmpty)
            throw new ArgumentException("LootTableId vacío.", nameof(lootTableId));

        VisualKey = visualKey;
        CombatEnabled = combatEnabled || combat is not null;
        Behavior = behavior ?? new CreatureBehaviorDefinition();
        Combat = CombatEnabled ? combat ?? new CreatureCombatDefinition() : null;
        LootTableId = lootTableId;
        LootMode = lootMode;
        Services = DefinitionModelGuards.CopyDefinitionHooks(services, nameof(services));
        EventHooks = DefinitionModelGuards.CopyDefinitionHooks(eventHooks, nameof(eventHooks));
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        Collision = collision;
    }

    public ContentKey VisualKey { get; }
    public bool CombatEnabled { get; }
    public CreatureBehaviorDefinition Behavior { get; }
    public CreatureCombatDefinition? Combat { get; }
    public DefinitionId? LootTableId { get; }
    public LootMode LootMode { get; }
    public Dictionary<string, DefinitionId> Services { get; }
    public Dictionary<string, DefinitionId> EventHooks { get; }
    public Dictionary<string, string> Metadata { get; }
    public EntityCollisionProfileDefinition? Collision { get; }
}
