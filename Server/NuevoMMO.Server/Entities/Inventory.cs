using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

/// <summary>
/// Estado autoritativo del inventario de una entidad. Esta clase conserva identidad,
/// capacidad y operaciones atómicas básicas; las reglas de stacking/permisos viven en InventorySystem.
/// </summary>
public sealed class Inventory
{
    private readonly List<ItemInstance> items = [];

    public Inventory(int maxSlots = int.MaxValue)
    {
        if (maxSlots < 1) throw new ArgumentOutOfRangeException(nameof(maxSlots));
        MaxSlots = maxSlots;
    }

    public IReadOnlyList<ItemInstance> Items => items;
    public int Count => items.Count;
    public int MaxSlots { get; private set; }
    public int FreeSlots => MaxSlots == int.MaxValue ? int.MaxValue : MaxSlots - items.Count;
    public bool IsFull => items.Count >= MaxSlots;

    public void SetCapacity(int maxSlots)
    {
        if (maxSlots < 1) throw new ArgumentOutOfRangeException(nameof(maxSlots));
        if (maxSlots < items.Count)
            throw new InvalidOperationException("La nueva capacidad no puede ser menor a los slots ocupados.");
        MaxSlots = maxSlots;
    }

    public ItemInstance Get(ItemInstanceId id)
        => TryGet(id, out var item)
            ? item!
            : throw new KeyNotFoundException($"No existe el item {id.Value} en el inventario.");

    public bool TryGet(ItemInstanceId id, out ItemInstance? item)
    {
        item = null;
        if (id.Value == Guid.Empty) return false;
        item = items.FirstOrDefault(value => value.UniqueId == id);
        return item is not null;
    }

    public bool Contains(ItemInstanceId id) => TryGet(id, out _);

    public IReadOnlyList<ItemInstance> Find(DefinitionId definitionId)
    {
        if (definitionId.IsEmpty) throw new ArgumentException("DefinitionId vacío.", nameof(definitionId));
        return items.Where(item => item.DefinitionId == definitionId).ToArray();
    }

    public long QuantityOf(DefinitionId definitionId)
    {
        if (definitionId.IsEmpty) throw new ArgumentException("DefinitionId vacío.", nameof(definitionId));
        return items.Where(item => item.DefinitionId == definitionId).Sum(static item => (long)item.Quantity);
    }

    public void Add(ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.UniqueId.Value == Guid.Empty) throw new ArgumentException("ItemInstanceId vacío.", nameof(item));
        if (Contains(item.UniqueId))
            throw new InvalidOperationException($"El item {item.UniqueId.Value} ya existe en el inventario.");
        if (IsFull) throw new InvalidOperationException("Inventario lleno.");
        items.Add(item);
    }

    public bool Remove(ItemInstanceId id)
    {
        if (id.Value == Guid.Empty) return false;
        var index = items.FindIndex(item => item.UniqueId == id);
        if (index < 0) return false;
        items.RemoveAt(index);
        return true;
    }

    public ItemInstance Take(ItemInstanceId id)
    {
        var item = Get(id);
        items.Remove(item);
        return item;
    }

    public void Clear() => items.Clear();
}
