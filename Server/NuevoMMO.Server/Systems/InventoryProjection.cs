using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public static class InventoryProjection
{
    public static InventorySnapshotPacket Create(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var items = player.Inventory.Items
            .Select(item => new InventoryItemSnapshot(item.UniqueId, item.DefinitionId, item.Quantity, item.Durability))
            .ToArray();
        var equipped = player.Equipment.Entries
            .Select(pair => new EquippedItemSnapshot(pair.Key.Slot, (byte)Math.Clamp(pair.Key.Index, 0, 255), pair.Value))
            .ToArray();
        return new InventorySnapshotPacket(items, equipped);
    }
}
