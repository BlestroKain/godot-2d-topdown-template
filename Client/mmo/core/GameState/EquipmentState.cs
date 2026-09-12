using NuevoMMO.Core;

namespace NuevoMMO.Client;

public readonly record struct EquipmentSlotKey(EquipmentSlot Slot, int Index = 0);

public sealed class EquipmentState
{
    private readonly Dictionary<EquipmentSlotKey, InventorySlotState> worn = [];

    public IReadOnlyDictionary<EquipmentSlotKey, InventorySlotState> Worn => worn;

    public void Equip(EquipmentSlot slot, int index, InventorySlotState item)
    {
        if (slot == EquipmentSlot.None) throw new ArgumentOutOfRangeException(nameof(slot));
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        ArgumentNullException.ThrowIfNull(item);
        worn[new EquipmentSlotKey(slot, index)] = item;
    }

    public bool Unequip(EquipmentSlot slot, int index = 0) => worn.Remove(new EquipmentSlotKey(slot, index));

    public bool IsEquipped(EquipmentSlot slot, int index = 0) => worn.ContainsKey(new EquipmentSlotKey(slot, index));

    public bool Contains(ItemInstanceId itemId)
        => itemId.Value != Guid.Empty && worn.Values.Any(item => item.ItemId == itemId);

    public void Clear() => worn.Clear();
}
