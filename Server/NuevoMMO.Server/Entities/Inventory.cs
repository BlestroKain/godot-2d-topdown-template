using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class Inventory
{
    private readonly List<ItemInstance> items = [];
    public IReadOnlyList<ItemInstance> Items => items;
    public void Add(ItemInstance item) => items.Add(item);
    public bool Remove(ItemInstanceId id) => items.RemoveAll(item => item.UniqueId == id) > 0;
}
