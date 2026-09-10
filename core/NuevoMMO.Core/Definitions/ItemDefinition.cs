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
        Dictionary<string, string>? metadata = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));
        if (rarity < 0) throw new ArgumentOutOfRangeException(nameof(rarity));
        if (basePrice < 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
        if (groundDespawnMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(groundDespawnMilliseconds));
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

    /// <summary>
    /// Todas las propiedades que este tipo de objeto puede generar.
    /// Se conserva como vista rápida/compatibilidad; PropertyRanges tiene precedencia
    /// cuando el rango es específico de este objeto.
    /// </summary>
    public DefinitionId[] PropertyIds { get; }

    /// <summary>
    /// Rango de roll por propiedad para este objeto concreto. Esto permite que la misma
    /// propiedad (STR, Pureza, Conductividad, etc.) use rangos diferentes por definición.
    /// </summary>
    public Dictionary<DefinitionId, NumericRange> PropertyRanges { get; }

    public long GroundDespawnMilliseconds { get; }
    public Dictionary<string, string> Metadata { get; }
}
