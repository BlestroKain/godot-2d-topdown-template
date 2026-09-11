using NuevoMMO.Core;

namespace NuevoMMO.Client;

public enum EntityLabelKind : byte
{
    Header,
    Footer,
    Name,
    ChatBubble
}

public interface IClientEntity
{
    EntityId Id { get; }
    EntityKind Kind { get; }
    string DisplayName { get; }
    ContentKey VisualKey { get; }
    Vector2Data Position { get; }
    Vector2Data Velocity { get; }
    Direction Facing { get; }
    MapInstanceId MapInstance { get; }
    DefinitionId? DefinitionId { get; }
    EntityState State { get; }
    bool InView { get; }
}

public interface IClientLivingEntity : IClientEntity
{
    bool IsMoving { get; }
}

public interface IClientPlayer : IClientLivingEntity
{
    EntityId? TargetId { get; }
    bool HasParty { get; }
    bool TryTarget(EntityId id);
    void ClearTarget();
}

public interface IClientResource : IClientEntity
{
    DefinitionId ResourceDefinition { get; }
}

public interface IClientStatus
{
    DefinitionId EffectId { get; }
    long RemainingMilliseconds { get; }
    long TotalMilliseconds { get; }
    bool IsActive { get; }
}

public interface IHotbarSlot
{
    int Index { get; }
    DefinitionId? BoundId { get; }
    HotbarBindingKind Kind { get; }
    bool IsEmpty { get; }
}

public enum HotbarBindingKind : byte
{
    Empty,
    Item,
    Technique
}

public interface IClientItem
{
    ItemInstanceId InstanceId { get; }
    DefinitionId DefinitionId { get; }
    int Quantity { get; }
}

public interface IClientMap
{
    DefinitionId Definition { get; }
    MapInstanceId Instance { get; }
    BoundsData Bounds { get; }
    IReadOnlyList<IActionMessage> ActionMessages { get; }
}

public interface IActionMessage
{
    Vector2Data Position { get; }
    string Text { get; }
}
