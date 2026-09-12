using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record InventoryItemSnapshot(ItemInstanceId ItemId, DefinitionId DefinitionId, int Quantity, int Durability);

public sealed record EquippedItemSnapshot(EquipmentSlot Slot, byte Index, ItemInstanceId ItemId);

public sealed record InventorySnapshotPacket(InventoryItemSnapshot[] Items, EquippedItemSnapshot[] Equipped) : IPacket;

public sealed record EquipItemRequest(ItemInstanceId ItemId) : IPacket;

public sealed record UnequipItemRequest(ItemInstanceId ItemId) : IPacket;

/// <summary>
/// Reorders an existing authoritative inventory stack. TargetIndex is an insertion index in
/// the compact server-owned stack list; it is not a free-form client position.
/// </summary>
public sealed record MoveInventoryItemRequest(ItemInstanceId ItemId, int TargetIndex) : IPacket;
