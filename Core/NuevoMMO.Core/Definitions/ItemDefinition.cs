namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de un tipo de objeto. Contiene toda la configuración editable
/// del objeto; una ItemInstance solo conserva identidad, cantidad, durabilidad y rolls.
/// </summary>
public sealed record ItemDefinition : GameDefinition
{
    public ItemDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey visualKey,
        DefinitionId[]? propertyIds = null,
        ItemKind kind = ItemKind.Generic,
        int rarity = 0,
        long basePrice = 0,
        ItemPermissionsDefinition? permissions = null,
        ItemStackDefinition? stacking = null,
        ItemEquipmentDefinition? equipment = null,
        ItemUseDefinition? use = null,
        Dictionary<DefinitionId, NumericRange>? propertyRanges = null,
        long groundDespawnMilliseconds = 0,
        Dictionary<string, string>? metadata = null,
        ItemCombatDefinition? combat = null,
        ItemConsumableDefinition? consumable = null,
        ItemRequirementDefinition? requirements = null,
        float dropChanceOnDeathPercent = 0,
        ContentKey? toolKey = null,
        DefinitionId[]? passiveEffectIds = null,
        Dictionary<string, DefinitionId>? references = null,
        Dictionary<string, ContentKey>? visualOverrides = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));
        if (rarity < 0) throw new ArgumentOutOfRangeException(nameof(rarity));
        if (basePrice < 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
        if (groundDespawnMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(groundDespawnMilliseconds));
        if (!float.IsFinite(dropChanceOnDeathPercent) || dropChanceOnDeathPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(dropChanceOnDeathPercent));
        if (toolKey is { } tool && tool.IsEmpty) throw new ArgumentException("ToolKey vacío.", nameof(toolKey));
        if (kind == ItemKind.Equipment && equipment is null)
            throw new ArgumentException("Un objeto Equipment requiere ItemEquipmentDefinition.", nameof(equipment));
        if (equipment is not null && kind != ItemKind.Equipment)
            throw new ArgumentException("ItemEquipmentDefinition solo es válido para ItemKind.Equipment.", nameof(equipment));

        var ids = DefinitionModelGuards.CopyIds(propertyIds, nameof(propertyIds));
        PropertyRanges = DefinitionModelGuards.CopyRanges(propertyRanges, nameof(propertyRanges));

        VisualKey = visualKey;
        Kind = kind;
        Rarity = rarity;
        BasePrice = basePrice;
        Permissions = permissions ?? new ItemPermissionsDefinition();
        Stacking = stacking ?? new ItemStackDefinition();
        Equipment = equipment;
        Use = use;
        Combat = combat;
        Consumable = consumable;
        Requirements = requirements ?? new ItemRequirementDefinition();
        DropChanceOnDeathPercent = dropChanceOnDeathPercent;
        ToolKey = toolKey;
        PassiveEffectIds = DefinitionModelGuards.CopyIds(passiveEffectIds, nameof(passiveEffectIds));
        References = DefinitionModelGuards.CopyDefinitionHooks(references, nameof(references));
        VisualOverrides = CopyVisualOverrides(visualOverrides);
        GroundDespawnMilliseconds = groundDespawnMilliseconds;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        PropertyIds = ids.Concat(PropertyRanges.Keys).Distinct().ToArray();
    }

    public ContentKey VisualKey { get; }
    public ItemKind Kind { get; }
    public int Rarity { get; }
    public long BasePrice { get; }
    public ItemPermissionsDefinition Permissions { get; }
    public ItemStackDefinition Stacking { get; }
    public ItemEquipmentDefinition? Equipment { get; }
    public ItemUseDefinition? Use { get; }
    public ItemCombatDefinition? Combat { get; }
    public ItemConsumableDefinition? Consumable { get; }
    public ItemRequirementDefinition Requirements { get; }
    public float DropChanceOnDeathPercent { get; }
    public ContentKey? ToolKey { get; }
    public DefinitionId[] PassiveEffectIds { get; }

    /// <summary>
    /// Referencias extensibles a contenido relacionado: projectile, event, destroy-event, etc.
    /// Cuando exista una Definition especializada puede reemplazarse la clave genérica por un campo tipado.
    /// </summary>
    public Dictionary<string, DefinitionId> References { get; }

    /// <summary>
    /// Variantes visuales semánticas: equipped, attack, use, male-paperdoll, female-paperdoll, etc.
    /// El servidor solo conserva ContentKeys; el cliente resuelve assets.
    /// </summary>
    public Dictionary<string, ContentKey> VisualOverrides { get; }

    public DefinitionId[] PropertyIds { get; }
    public Dictionary<DefinitionId, NumericRange> PropertyRanges { get; }
    public long GroundDespawnMilliseconds { get; }
    public Dictionary<string, string> Metadata { get; }

    private static Dictionary<string, ContentKey> CopyVisualOverrides(Dictionary<string, ContentKey>? source)
    {
        var result = new Dictionary<string, ContentKey>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return result;
        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("VisualOverrides contiene una clave vacía.", nameof(source));
            if (value.IsEmpty) throw new ArgumentException("VisualOverrides contiene un ContentKey vacío.", nameof(source));
            result.Add(key.Trim(), value);
        }
        return result;
    }
}
