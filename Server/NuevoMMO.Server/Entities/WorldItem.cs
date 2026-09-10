using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

/// <summary>
/// Item runtime que existe físicamente en el mundo. Conserva la ItemInstance completa para no
/// perder cantidad, durabilidad, propiedades ni metadata al pasar inventario -> suelo -> inventario.
/// Las reservas son exclusivamente autoritativas del servidor y no forman parte del EntityState.
/// </summary>
public sealed class WorldItem : Entity
{
    public WorldItem(
        EntityId id,
        ItemInstance item,
        MapInstanceId mapInstance,
        Vector2Data position,
        ContentKey visualKey,
        string displayName,
        long despawnAtMilliseconds = 0)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        if (despawnAtMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(despawnAtMilliseconds));
        DespawnAtMilliseconds = despawnAtMilliseconds;
    }

    /// <summary>
    /// Compatibilidad con llamadas antiguas. El código nuevo debe entregar la ItemInstance completa.
    /// </summary>
    public WorldItem(
        EntityId id,
        DefinitionId definition,
        ItemInstanceId itemInstance,
        MapInstanceId mapInstance,
        Vector2Data position,
        ContentKey visualKey,
        string displayName)
        : this(id, new ItemInstance(itemInstance, definition, 1, 0), mapInstance, position, visualKey, displayName)
    {
    }

    public ItemInstance Item { get; }
    public DefinitionId DefinitionId => Item.DefinitionId;
    public ItemInstanceId ItemInstanceId => Item.UniqueId;
    public EntityId? ReservedFor { get; private set; }
    public long ReservationExpiresAtMilliseconds { get; private set; }
    public long DespawnAtMilliseconds { get; }
    public bool PickedUp { get; private set; }

    public bool IsExpired(long nowMilliseconds)
    {
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        return DespawnAtMilliseconds > 0 && nowMilliseconds >= DespawnAtMilliseconds;
    }

    public bool IsAvailableTo(EntityId entity, long nowMilliseconds)
    {
        if (entity.Value <= 0) return false;
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (PickedUp || IsExpired(nowMilliseconds)) return false;
        ExpireReservation(nowMilliseconds);
        return ReservedFor is null || ReservedFor == entity;
    }

    public bool TryReserve(EntityId entity, long nowMilliseconds, int reservationMilliseconds = 1_500)
    {
        if (entity.Value <= 0) throw new ArgumentException("EntityId inválido.", nameof(entity));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (reservationMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(reservationMilliseconds));
        if (!IsAvailableTo(entity, nowMilliseconds)) return false;

        ReservedFor = entity;
        ReservationExpiresAtMilliseconds = checked(nowMilliseconds + reservationMilliseconds);
        return true;
    }

    public void ReleaseReservation(EntityId entity)
    {
        if (ReservedFor != entity) return;
        ReservedFor = null;
        ReservationExpiresAtMilliseconds = 0;
    }

    public void MarkPickedUp(EntityId entity, long nowMilliseconds)
    {
        if (!IsAvailableTo(entity, nowMilliseconds) || ReservedFor != entity)
            throw new InvalidOperationException("El item no está reservado por la entidad indicada.");
        PickedUp = true;
        ReservedFor = null;
        ReservationExpiresAtMilliseconds = 0;
    }

    public override EntityState ToState() => new WorldItemState(Id, DefinitionId, ItemInstanceId, MapInstanceId,
        Position, Direction, VisualKey, DisplayName);

    private void ExpireReservation(long nowMilliseconds)
    {
        if (ReservedFor is null || ReservationExpiresAtMilliseconds <= 0 || nowMilliseconds < ReservationExpiresAtMilliseconds)
            return;
        ReservedFor = null;
        ReservationExpiresAtMilliseconds = 0;
    }
}
