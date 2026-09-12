using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed record InventorySlotState(
    int Slot,
    ItemInstanceId ItemId,
    DefinitionId DefinitionId,
    int Quantity,
    int Durability);

/// <summary>
/// Copia local del inventario. El servidor es la autoridad; esto solo proyecta lo recibido.
/// </summary>
public sealed class InventoryState
{
    private readonly List<InventorySlotState> slots = [];

    public IReadOnlyList<InventorySlotState> Slots => slots;
    public int Count => slots.Count;

    public void Replace(IEnumerable<InventorySlotState> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();
        if (copy.Any(static slot =>
                slot.Slot < 0 ||
                slot.ItemId.Value == Guid.Empty ||
                slot.DefinitionId.IsEmpty ||
                slot.Quantity < 1 ||
                slot.Durability < 0))
            throw new ArgumentException("Slot de inventario inválido.", nameof(values));

        slots.Clear();
        slots.AddRange(copy);
    }

    public void Clear() => slots.Clear();
}
