namespace NuevoMMO.Core;

public readonly record struct NumericRange
{
    public NumericRange(float minimum, float maximum)
    {
        if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum > maximum)
            throw new ArgumentException("Rango numérico inválido.");
        Minimum = minimum;
        Maximum = maximum;
    }

    public float Minimum { get; }
    public float Maximum { get; }
    public bool Contains(float value) => float.IsFinite(value) && value >= Minimum && value <= Maximum;
    public static NumericRange Fixed(float value) => new(value, value);
}

public enum ItemKind : byte
{
    Generic,
    Equipment,
    Consumable,
    Currency,
    Quest,
    Tool,
    Container,
    Material
}

public enum EquipmentSlot : byte
{
    None,
    Head,
    Chest,
    Hands,
    Boots,
    Belt,
    Cape,
    Ring,
    Amulet,
    Weapon,
    OffHand,
    Trophy
}

public enum WeaponFamily : byte
{
    None,
    Short,
    OneHanded,
    Long,
    Heavy,
    Projectile,
    Thrown,
    Catalyst,
    Instrument
}

public enum CreatureMovementMode : byte
{
    Stationary,
    Wander,
    Patrol
}

public enum CreatureTargetPriority : byte
{
    Nearest,
    HighestDamage,
    LowestHealth,
    Random
}

public enum LootMode : byte
{
    Shared,
    Individual
}

public enum VitalId : byte
{
    Health,
    Mana
}

public sealed record ItemPermissionsDefinition(
    bool CanDrop = true,
    bool CanTrade = true,
    bool CanSell = true,
    bool CanBank = true,
    bool CanGuildBank = true,
    bool CanBag = true);

public sealed record ItemStackDefinition
{
    public ItemStackDefinition(bool stackable = false, int maxInventoryStack = 1, int maxBankStack = 1)
    {
        if (maxInventoryStack < 1) throw new ArgumentOutOfRangeException(nameof(maxInventoryStack));
        if (maxBankStack < 1) throw new ArgumentOutOfRangeException(nameof(maxBankStack));
        if (!stackable && (maxInventoryStack != 1 || maxBankStack != 1))
            throw new ArgumentException("Un objeto no apilable debe usar stack máximo 1.");
        Stackable = stackable;
        MaxInventoryStack = maxInventoryStack;
        MaxBankStack = maxBankStack;
    }

    public bool Stackable { get; }
    public int MaxInventoryStack { get; }
    public int MaxBankStack { get; }
}

public sealed record ItemEquipmentDefinition
{
    public ItemEquipmentDefinition(
        EquipmentSlot slot,
        WeaponFamily weaponFamily = WeaponFamily.None,
        bool twoHanded = false,
        int? maxDurability = null,
        Dictionary<StatId, float>? flatStats = null,
        Dictionary<StatId, float>? percentStats = null,
        Dictionary<string, float>? combatModifiers = null)
    {
        if (slot == EquipmentSlot.None)
            throw new ArgumentException("Un ItemEquipmentDefinition requiere slot.", nameof(slot));
        if (maxDurability is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDurability), "MaxDurability debe ser positivo cuando se define.");
        Slot = slot;
        WeaponFamily = weaponFamily;
        TwoHanded = twoHanded;
        MaxDurability = maxDurability;
        FlatStats = DefinitionModelGuards.CopyFinite(flatStats, nameof(flatStats));
        PercentStats = DefinitionModelGuards.CopyFinite(percentStats, nameof(percentStats));
        CombatModifiers = DefinitionModelGuards.CopyFinite(combatModifiers, nameof(combatModifiers));
    }

    public EquipmentSlot Slot { get; }
    public WeaponFamily WeaponFamily { get; }
    public bool TwoHanded { get; }
    public int? MaxDurability { get; }
    public Dictionary<StatId, float> FlatStats { get; }
    public Dictionary<StatId, float> PercentStats { get; }
    public Dictionary<string, float> CombatModifiers { get; }
}

public sealed record ItemUseDefinition
{
    public ItemUseDefinition(
        DefinitionId? techniqueId = null,
        int cooldownMilliseconds = 0,
        string? cooldownGroup = null,
        bool ignoreGlobalCooldown = false,
        bool ignoreCooldownReduction = false,
        bool quickCast = false,
        bool singleUse = false,
        Dictionary<string, float>? parameters = null)
    {
        if (techniqueId is { } technique && technique.IsEmpty)
            throw new ArgumentException("TechniqueId vacío.", nameof(techniqueId));
        if (cooldownMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(cooldownMilliseconds));
        TechniqueId = techniqueId;
        CooldownMilliseconds = cooldownMilliseconds;
        CooldownGroup = cooldownGroup?.Trim() ?? string.Empty;
        IgnoreGlobalCooldown = ignoreGlobalCooldown;
        IgnoreCooldownReduction = ignoreCooldownReduction;
        QuickCast = quickCast;
        SingleUse = singleUse;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public DefinitionId? TechniqueId { get; }
    public int CooldownMilliseconds { get; }
    public string CooldownGroup { get; }
    public bool IgnoreGlobalCooldown { get; }
    public bool IgnoreCooldownReduction { get; }
    public bool QuickCast { get; }
    public bool SingleUse { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record CreatureBehaviorDefinition
{
    public CreatureBehaviorDefinition(
        bool aggressive = false,
        bool attackAllies = false,
        bool swarm = false,
        byte fleeHealthPercentage = 0,
        CreatureTargetPriority targetPriority = CreatureTargetPriority.Nearest,
        CreatureMovementMode movement = CreatureMovementMode.Wander,
        float sightRange = 0,
        float resetRadius = 0,
        bool npcVsNpcEnabled = false)
    {
        if (fleeHealthPercentage > 100) throw new ArgumentOutOfRangeException(nameof(fleeHealthPercentage));
        if (!float.IsFinite(sightRange) || sightRange < 0) throw new ArgumentOutOfRangeException(nameof(sightRange));
        if (!float.IsFinite(resetRadius) || resetRadius < 0) throw new ArgumentOutOfRangeException(nameof(resetRadius));
        Aggressive = aggressive;
        AttackAllies = attackAllies;
        Swarm = swarm;
        FleeHealthPercentage = fleeHealthPercentage;
        TargetPriority = targetPriority;
        Movement = movement;
        SightRange = sightRange;
        ResetRadius = resetRadius;
        NpcVsNpcEnabled = npcVsNpcEnabled;
    }

    public bool Aggressive { get; }
    public bool AttackAllies { get; }
    public bool Swarm { get; }
    public byte FleeHealthPercentage { get; }
    public CreatureTargetPriority TargetPriority { get; }
    public CreatureMovementMode Movement { get; }
    public float SightRange { get; }
    public float ResetRadius { get; }
    public bool NpcVsNpcEnabled { get; }
}

public sealed record CreatureCombatDefinition
{
    public CreatureCombatDefinition(
        int level = 1,
        long experience = 0,
        float baseDamage = 0,
        Element basicAttackElement = Element.Neutral,
        float criticalChancePercent = 0,
        float criticalMultiplier = 1.5f,
        float tenacity = 0,
        int attackIntervalMilliseconds = 0,
        int techniqueIntervalMilliseconds = 0,
        Dictionary<StatId, float>? stats = null,
        Dictionary<VitalId, float>? maxVitals = null,
        Dictionary<VitalId, float>? vitalRegeneration = null,
        Dictionary<StatId, float>? scaling = null,
        DefinitionId[]? techniqueIds = null,
        DefinitionId[]? immunityEffectIds = null,
        Dictionary<string, float>? parameters = null)
    {
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience));
        if (!float.IsFinite(baseDamage) || baseDamage < 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
        if (!float.IsFinite(criticalChancePercent) || criticalChancePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(criticalChancePercent));
        if (!float.IsFinite(criticalMultiplier) || criticalMultiplier < 0)
            throw new ArgumentOutOfRangeException(nameof(criticalMultiplier));
        if (!float.IsFinite(tenacity) || tenacity < 0) throw new ArgumentOutOfRangeException(nameof(tenacity));
        if (attackIntervalMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(attackIntervalMilliseconds));
        if (techniqueIntervalMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(techniqueIntervalMilliseconds));
        Level = level;
        Experience = experience;
        BaseDamage = baseDamage;
        BasicAttackElement = basicAttackElement;
        CriticalChancePercent = criticalChancePercent;
        CriticalMultiplier = criticalMultiplier;
        Tenacity = tenacity;
        AttackIntervalMilliseconds = attackIntervalMilliseconds;
        TechniqueIntervalMilliseconds = techniqueIntervalMilliseconds;
        Stats = DefinitionModelGuards.CopyFinite(stats, nameof(stats));
        MaxVitals = DefinitionModelGuards.CopyNonNegative(maxVitals, nameof(maxVitals));
        VitalRegeneration = DefinitionModelGuards.CopyFinite(vitalRegeneration, nameof(vitalRegeneration));
        Scaling = DefinitionModelGuards.CopyFinite(scaling, nameof(scaling));
        TechniqueIds = DefinitionModelGuards.CopyIds(techniqueIds, nameof(techniqueIds));
        ImmunityEffectIds = DefinitionModelGuards.CopyIds(immunityEffectIds, nameof(immunityEffectIds));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public int Level { get; }
    public long Experience { get; }
    public float BaseDamage { get; }
    public Element BasicAttackElement { get; }
    public float CriticalChancePercent { get; }
    public float CriticalMultiplier { get; }
    public float Tenacity { get; }
    public int AttackIntervalMilliseconds { get; }
    public int TechniqueIntervalMilliseconds { get; }
    public Dictionary<StatId, float> Stats { get; }
    public Dictionary<VitalId, float> MaxVitals { get; }
    public Dictionary<VitalId, float> VitalRegeneration { get; }
    public Dictionary<StatId, float> Scaling { get; }
    public DefinitionId[] TechniqueIds { get; }
    public DefinitionId[] ImmunityEffectIds { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record ResourceHarvestDefinition
{
    public ResourceHarvestDefinition(
        DefinitionId? lootTableId = null,
        DefinitionId? requiredProfessionId = null,
        int requiredProfessionLevel = 0,
        ContentKey? requiredToolKey = null,
        NumericRange? healthRange = null,
        int respawnMilliseconds = 0,
        bool blocksMovementWhileAvailable = false,
        bool blocksMovementWhileExhausted = false,
        Dictionary<string, float>? parameters = null)
    {
        if (lootTableId is { } loot && loot.IsEmpty) throw new ArgumentException("LootTableId vacío.", nameof(lootTableId));
        if (requiredProfessionId is { } profession && profession.IsEmpty)
            throw new ArgumentException("RequiredProfessionId vacío.", nameof(requiredProfessionId));
        if (requiredProfessionLevel < 0) throw new ArgumentOutOfRangeException(nameof(requiredProfessionLevel));
        if (requiredProfessionLevel > 0 && requiredProfessionId is null)
            throw new ArgumentException("RequiredProfessionLevel requiere RequiredProfessionId.", nameof(requiredProfessionLevel));
        if (requiredToolKey is { } tool && tool.IsEmpty) throw new ArgumentException("RequiredToolKey vacío.", nameof(requiredToolKey));
        if (respawnMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(respawnMilliseconds));
        LootTableId = lootTableId;
        RequiredProfessionId = requiredProfessionId;
        RequiredProfessionLevel = requiredProfessionLevel;
        RequiredToolKey = requiredToolKey;
        HealthRange = healthRange;
        RespawnMilliseconds = respawnMilliseconds;
        BlocksMovementWhileAvailable = blocksMovementWhileAvailable;
        BlocksMovementWhileExhausted = blocksMovementWhileExhausted;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public DefinitionId? LootTableId { get; }
    public DefinitionId? RequiredProfessionId { get; }
    public int RequiredProfessionLevel { get; }
    public ContentKey? RequiredToolKey { get; }
    public NumericRange? HealthRange { get; }
    public int RespawnMilliseconds { get; }
    public bool BlocksMovementWhileAvailable { get; }
    public bool BlocksMovementWhileExhausted { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record LootEntryDefinition
{
    public LootEntryDefinition(
        DefinitionId itemId,
        float chancePercent,
        int minimumQuantity = 1,
        int maximumQuantity = 1,
        Dictionary<DefinitionId, NumericRange>? propertyOverrides = null)
    {
        if (itemId.IsEmpty) throw new ArgumentException("ItemId vacío.", nameof(itemId));
        if (!float.IsFinite(chancePercent) || chancePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(chancePercent));
        if (minimumQuantity < 1) throw new ArgumentOutOfRangeException(nameof(minimumQuantity));
        if (maximumQuantity < minimumQuantity) throw new ArgumentOutOfRangeException(nameof(maximumQuantity));
        ItemId = itemId;
        ChancePercent = chancePercent;
        MinimumQuantity = minimumQuantity;
        MaximumQuantity = maximumQuantity;
        PropertyOverrides = DefinitionModelGuards.CopyRanges(propertyOverrides, nameof(propertyOverrides));
    }

    public DefinitionId ItemId { get; }
    public float ChancePercent { get; }
    public int MinimumQuantity { get; }
    public int MaximumQuantity { get; }
    public Dictionary<DefinitionId, NumericRange> PropertyOverrides { get; }
}

internal static class DefinitionModelGuards
{
    public static Dictionary<TKey, float> CopyFinite<TKey>(Dictionary<TKey, float>? source, string parameterName)
        where TKey : notnull
    {
        var result = source is null ? [] : new Dictionary<TKey, float>(source);
        if (result.Values.Any(value => !float.IsFinite(value)))
            throw new ArgumentException("El diccionario contiene un valor no finito.", parameterName);
        return result;
    }

    public static Dictionary<TKey, float> CopyNonNegative<TKey>(Dictionary<TKey, float>? source, string parameterName)
        where TKey : notnull
    {
        var result = CopyFinite(source, parameterName);
        if (result.Values.Any(value => value < 0))
            throw new ArgumentException("El diccionario contiene un valor negativo.", parameterName);
        return result;
    }

    public static DefinitionId[] CopyIds(IEnumerable<DefinitionId>? ids, string parameterName)
    {
        var result = ids?.ToArray() ?? [];
        if (result.Any(static id => id.IsEmpty)) throw new ArgumentException("La colección contiene un DefinitionId vacío.", parameterName);
        if (result.Distinct().Count() != result.Length) throw new ArgumentException("La colección contiene DefinitionId duplicados.", parameterName);
        return result;
    }

    public static Dictionary<DefinitionId, NumericRange> CopyRanges(
        Dictionary<DefinitionId, NumericRange>? source,
        string parameterName)
    {
        var result = source is null ? [] : new Dictionary<DefinitionId, NumericRange>(source);
        if (result.Keys.Any(static id => id.IsEmpty))
            throw new ArgumentException("El diccionario contiene un DefinitionId vacío.", parameterName);
        return result;
    }

    public static Dictionary<string, DefinitionId> CopyDefinitionHooks(
        Dictionary<string, DefinitionId>? source,
        string parameterName)
    {
        var result = new Dictionary<string, DefinitionId>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return result;
        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("El diccionario contiene una clave vacía.", parameterName);
            if (value.IsEmpty) throw new ArgumentException("El diccionario contiene un DefinitionId vacío.", parameterName);
            result.Add(key.Trim(), value);
        }
        return result;
    }
}
