using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class ClientItem : IClientItem
{
    public ClientItem(ItemInstanceId instanceId, DefinitionId definitionId, int quantity)
    {
        if (instanceId.Value == Guid.Empty) throw new ArgumentException("ItemInstanceId vacío.", nameof(instanceId));
        if (definitionId.IsEmpty) throw new ArgumentException("DefinitionId vacío.", nameof(definitionId));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        InstanceId = instanceId;
        DefinitionId = definitionId;
        Quantity = quantity;
    }

    public ItemInstanceId InstanceId { get; }
    public DefinitionId DefinitionId { get; }
    public int Quantity { get; private set; }

    public void SetQuantity(int quantity)
    {
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
    }

    public static ClientItem FromSlot(InventorySlotState slot)
        => new(slot.ItemId, slot.DefinitionId, slot.Quantity);
}
