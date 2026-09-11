using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class HotbarState
{
    public const int SlotCount = 6;
    private readonly HotbarSlot[] slots = Enumerable.Range(0, SlotCount).Select(static index => new HotbarSlot(index)).ToArray();

    public IReadOnlyList<IHotbarSlot> Slots => slots;

    public HotbarSlot this[int index] => slots[index];

    public void Bind(int index, HotbarBindingKind kind, DefinitionId definitionId)
    {
        if (index < 0 || index >= SlotCount) throw new ArgumentOutOfRangeException(nameof(index));
        slots[index].Bind(kind, definitionId);
    }

    public void Swap(int left, int right)
    {
        if (left < 0 || left >= SlotCount) throw new ArgumentOutOfRangeException(nameof(left));
        if (right < 0 || right >= SlotCount) throw new ArgumentOutOfRangeException(nameof(right));
        if (left == right) return;
        var a = slots[left];
        var b = slots[right];
        var aKind = a.Kind;
        var aId = a.BoundId;
        var bKind = b.Kind;
        var bId = b.BoundId;
        a.Clear();
        b.Clear();
        if (bKind != HotbarBindingKind.Empty && bId is { } bBound) a.Bind(bKind, bBound);
        if (aKind != HotbarBindingKind.Empty && aId is { } aBound) b.Bind(aKind, aBound);
    }

    public void Clear()
    {
        foreach (var slot in slots) slot.Clear();
    }
}
