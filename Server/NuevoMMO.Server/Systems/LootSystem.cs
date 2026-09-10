using NuevoMMO.Core;

namespace NuevoMMO.Server.Systems;

public interface ILootRandomSource
{
    float NextUnit();
    int NextInclusive(int minimum, int maximum);
    float NextRange(float minimum, float maximum);
}

public sealed class SharedLootRandomSource : ILootRandomSource
{
    public float NextUnit() => Random.Shared.NextSingle();
    public int NextInclusive(int minimum, int maximum)
        => minimum == maximum ? minimum : Random.Shared.Next(minimum, checked(maximum + 1));
    public float NextRange(float minimum, float maximum)
        => minimum == maximum ? minimum : minimum + (maximum - minimum) * Random.Shared.NextSingle();
}

public sealed record LootRoll(ItemInstance Item, LootEntryDefinition? SourceEntry);

/// <summary>
/// Generador autoritativo de loot. Adapta el concepto de drops con chance/cantidad de Intersect,
/// añadiendo los rolls variables propios de NuevoMMO.
/// </summary>
public sealed class LootSystem
{
    private readonly DefinitionRegistry definitions;
    private readonly ILootRandomSource random;

    public LootSystem(DefinitionRegistry definitions, ILootRandomSource? random = null)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.random = random ?? new SharedLootRandomSource();
    }

    public IReadOnlyList<LootRoll> Roll(DefinitionId lootTableId, float chanceMultiplier = 1f)
    {
        if (lootTableId.IsEmpty) throw new ArgumentException("LootTableId vacío.", nameof(lootTableId));
        return Roll(definitions.Get<LootTableDefinition>(lootTableId), chanceMultiplier);
    }

    public IReadOnlyList<LootRoll> Roll(LootTableDefinition table, float chanceMultiplier = 1f)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (!table.Enabled) return [];
        if (!float.IsFinite(chanceMultiplier) || chanceMultiplier < 0)
            throw new ArgumentOutOfRangeException(nameof(chanceMultiplier));

        var result = new List<LootRoll>();
        foreach (var entry in table.Entries)
        {
            var chance = Math.Clamp(entry.ChancePercent * chanceMultiplier, 0f, 100f);
            if (chance <= 0 || (chance < 100 && random.NextUnit() * 100f >= chance)) continue;
            var quantity = random.NextInclusive(entry.MinimumQuantity, entry.MaximumQuantity);
            result.Add(new LootRoll(CreateItem(entry.ItemId, quantity, entry.PropertyOverrides), entry));
        }

        // Compatibilidad con el array histórico ItemIds: los IDs que no tienen una entrada rica
        // se consideran drops garantizados de cantidad 1.
        var richIds = table.Entries.Select(static entry => entry.ItemId).ToHashSet();
        foreach (var itemId in table.ItemIds.Where(itemId => !richIds.Contains(itemId)))
            result.Add(new LootRoll(CreateItem(itemId, 1, null), null));

        return result;
    }

    public IReadOnlyList<LootRoll> Roll(MobDefinition mob, float chanceMultiplier = 1f)
    {
        ArgumentNullException.ThrowIfNull(mob);
        return mob.LootTableId is { } lootTable ? Roll(lootTable, chanceMultiplier) : [];
    }

    public IReadOnlyList<LootRoll> Roll(ResourceDefinition resource, float chanceMultiplier = 1f)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return resource.Harvest.LootTableId is { } lootTable ? Roll(lootTable, chanceMultiplier) : [];
    }

    public ItemInstance CreateItem(
        DefinitionId itemDefinitionId,
        int quantity,
        IReadOnlyDictionary<DefinitionId, NumericRange>? propertyOverrides = null)
    {
        if (itemDefinitionId.IsEmpty) throw new ArgumentException("ItemDefinitionId vacío.", nameof(itemDefinitionId));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        var definition = definitions.Get<ItemDefinition>(itemDefinitionId);
        if (!definition.Enabled) throw new InvalidOperationException($"El item {definition.Key} está deshabilitado.");

        var properties = new List<ItemPropertyValue>();
        foreach (var propertyId in definition.PropertyIds)
        {
            var range = ResolvePropertyRange(definition, propertyId, propertyOverrides);
            var value = random.NextRange(range.Minimum, range.Maximum);
            properties.Add(new ItemPropertyValue(propertyId, value));
        }

        var durability = definition.Equipment?.MaxDurability ?? 0;
        return new ItemInstance(
            new ItemInstanceId(Guid.NewGuid()),
            definition.Id,
            quantity,
            durability,
            properties.ToArray());
    }

    private NumericRange ResolvePropertyRange(
        ItemDefinition item,
        DefinitionId propertyId,
        IReadOnlyDictionary<DefinitionId, NumericRange>? overrides)
    {
        if (overrides is not null && overrides.TryGetValue(propertyId, out var overridden)) return overridden;
        if (item.PropertyRanges.TryGetValue(propertyId, out var itemRange)) return itemRange;
        return definitions.Get<ItemPropertyDefinition>(propertyId).DefaultRange;
    }
}
