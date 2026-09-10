namespace NuevoMMO.Core;

public sealed class ItemInstance
{
    public ItemInstance(ItemInstanceId uniqueId, DefinitionId definitionId, int quantity, int durability,
        IEnumerable<ItemPropertyValue>? properties = null, IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (uniqueId.Value == Guid.Empty) throw new ArgumentException("ItemInstanceId inválido.", nameof(uniqueId));
        if (definitionId.Value == Guid.Empty) throw new ArgumentException("DefinitionId inválido.", nameof(definitionId));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        UniqueId = uniqueId;
        DefinitionId = definitionId;
        Quantity = quantity;
        Durability = durability;
        Properties = properties?.ToArray() ?? [];
        Metadata = metadata is null ? new Dictionary<string, string>() : new Dictionary<string, string>(metadata);
    }

    public ItemInstanceId UniqueId { get; }
    public DefinitionId DefinitionId { get; }
    public int Quantity { get; set; }
    public int Durability { get; set; }
    public ItemPropertyValue[] Properties { get; set; }
    public Dictionary<string, string> Metadata { get; }
}
