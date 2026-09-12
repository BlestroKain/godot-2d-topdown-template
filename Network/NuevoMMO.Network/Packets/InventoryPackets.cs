using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record InventoryItemSnapshot(ItemInstanceId ItemId, DefinitionId DefinitionId, int Quantity, int Durability);

public sealed record EquippedItemSnapshot(EquipmentSlot Slot, byte Index, ItemInstanceId ItemId);

public sealed record InventorySnapshotPacket(InventoryItemSnapshot[] Items, EquippedItemSnapshot[] Equipped) : IPacket;

public sealed record EquipItemRequest(ItemInstanceId ItemId) : IPacket;

public sealed record UnequipItemRequest(ItemInstanceId ItemId) : IPacket;
