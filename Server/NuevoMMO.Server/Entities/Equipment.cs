using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public readonly record struct EquipmentPosition
{
    public EquipmentPosition(EquipmentSlot slot, int index = 0)
    {
        if (slot == EquipmentSlot.None) throw new ArgumentException("EquipmentSlot.None no es equipable.", nameof(slot));
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        Slot = slot;
        Index = index;
    }

    public EquipmentSlot Slot { get; }
    public int Index { get; }
    public override string ToString() => Index == 0 ? Slot.ToString() : $"{Slot}:{Index}";
}

/// <summary>
/// Estado autoritativo de equipo. La compatibilidad de slots, requisitos y conflictos
/// de dos manos se resuelven en EquipmentSystem; aquí solo se preservan invariantes de estado.
/// </summary>
public sealed class Equipment
{
    private readonly Dictionary<EquipmentPosition, ItemInstanceId> entries = [];

    public IReadOnlyDictionary<EquipmentPosition, ItemInstanceId> Entries => entries;
    public int Count => entries.Count;

    /// <summary>
    /// Vista textual de compatibilidad para tooling/debug. No debe usarse para reglas de gameplay.
    /// </summary>
    public IReadOnlyDictionary<string, ItemInstanceId> Slots
        => entries.ToDictionary(static pair => pair.Key.ToString(), static pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    public bool TryGet(EquipmentPosition position, out ItemInstanceId itemId)
        => entries.TryGetValue(position, out itemId);

    public bool TryGet(EquipmentSlot slot, out ItemInstanceId itemId, int index = 0)
        => TryGet(new EquipmentPosition(slot, index), out itemId);

    public ItemInstanceId Get(EquipmentPosition position)
        => entries.TryGetValue(position, out var itemId)
            ? itemId
            : throw new KeyNotFoundException($"No hay item equipado en {position}.");

    public bool Contains(ItemInstanceId itemId)
        => itemId.Value != Guid.Empty && entries.Values.Contains(itemId);

    public ItemInstanceId? Equip(EquipmentPosition position, ItemInstanceId itemId)
    {
        if (itemId.Value == Guid.Empty) throw new ArgumentException("ItemInstanceId vacío.", nameof(itemId));
        if (entries.Any(pair => pair.Key != position && pair.Value == itemId))
            throw new InvalidOperationException("La misma instancia de item no puede ocupar varios slots.");

        var replaced = entries.TryGetValue(position, out var previous) ? previous : (ItemInstanceId?)null;
        entries[position] = itemId;
        return replaced;
    }

    public bool Unequip(EquipmentPosition position, out ItemInstanceId itemId)
        => entries.Remove(position, out itemId);

    public bool Unequip(ItemInstanceId itemId, out EquipmentPosition position)
    {
        position = default;
        if (itemId.Value == Guid.Empty) return false;
        foreach (var pair in entries)
        {
            if (pair.Value != itemId) continue;
            position = pair.Key;
            entries.Remove(pair.Key);
            return true;
        }
        return false;
    }

    public IEnumerable<EquipmentPosition> PositionsFor(EquipmentSlot slot)
        => entries.Keys.Where(position => position.Slot == slot).OrderBy(static position => position.Index).ToArray();

    public void Clear() => entries.Clear();
}
