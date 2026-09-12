using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class EquipmentState
{
    private readonly Dictionary<EquipmentSlot, InventorySlotState> worn = [];

    public IReadOnlyDictionary<EquipmentSlot, InventorySlotState> Worn => worn;

    public void Equip(EquipmentSlot slot, InventorySlotState item)
    {
        if (slot == EquipmentSlot.None) throw new ArgumentOutOfRangeException(nameof(slot));
        ArgumentNullException.ThrowIfNull(item);
        worn[slot] = item;
    }

    public bool Unequip(EquipmentSlot slot) => worn.Remove(slot);

    public bool IsEquipped(EquipmentSlot slot) => worn.ContainsKey(slot);

    public bool Contains(ItemInstanceId itemId)
        => itemId.Value != Guid.Empty && worn.Values.Any(item => item.ItemId == itemId);

    public void Clear() => worn.Clear();
}
