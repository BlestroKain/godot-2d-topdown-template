using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class HotbarSlot : IHotbarSlot
{
    public HotbarSlot(int index, HotbarBindingKind kind = HotbarBindingKind.Empty, DefinitionId? boundId = null)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        Index = index;
        if (kind == HotbarBindingKind.Empty)
        {
            Kind = HotbarBindingKind.Empty;
            BoundId = null;
            return;
        }

        if (boundId is not { } id || id.IsEmpty)
            throw new ArgumentException("Hotbar ocupado requiere DefinitionId.", nameof(boundId));
        Kind = kind;
        BoundId = id;
    }

    public int Index { get; }
    public DefinitionId? BoundId { get; private set; }
    public HotbarBindingKind Kind { get; private set; }
    public bool IsEmpty => Kind == HotbarBindingKind.Empty || BoundId is null;

    public void Bind(HotbarBindingKind kind, DefinitionId definitionId)
    {
        if (kind == HotbarBindingKind.Empty) throw new ArgumentException("Use Clear() para vaciar el slot.", nameof(kind));
        if (definitionId.IsEmpty) throw new ArgumentException("DefinitionId vacío.", nameof(definitionId));
        Kind = kind;
        BoundId = definitionId;
    }

    public void Clear()
    {
        Kind = HotbarBindingKind.Empty;
        BoundId = null;
    }
}
