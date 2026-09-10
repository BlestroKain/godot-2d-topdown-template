using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public enum InteractionFailure
{
    None = 0,
    ActorUnavailable = 1,
    DifferentMap = 2,
    OutOfRange = 3,
    TargetUnavailable = 4,
    InventoryRejected = 5
}

public readonly record struct InteractionResult(
    bool Success,
    InteractionFailure Failure,
    string Message,
    IReadOnlyList<ItemInstanceId> AffectedItems)
{
    public static InteractionResult Ok(IReadOnlyList<ItemInstanceId> affected)
        => new(true, InteractionFailure.None, string.Empty, affected);

    public static InteractionResult Fail(InteractionFailure failure, string message)
        => new(false, failure, message, []);
}

/// <summary>
/// Reglas autoritativas de interacción inmediata. No elimina entidades del mapa ni envía packets:
/// valida y muta únicamente el estado de dominio involucrado. El WorldRuntime decide el despawn.
/// </summary>
public sealed class InteractionSystem
{
    private readonly InventorySystem inventory;
    private readonly float pickupRange;
    private readonly int reservationMilliseconds;

    public InteractionSystem(InventorySystem inventory, float pickupRange = 64f, int reservationMilliseconds = 1_500)
    {
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        if (!float.IsFinite(pickupRange) || pickupRange <= 0) throw new ArgumentOutOfRangeException(nameof(pickupRange));
        if (reservationMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(reservationMilliseconds));
        this.pickupRange = pickupRange;
        this.reservationMilliseconds = reservationMilliseconds;
    }

    public bool CanReach(Entity actor, Entity target, float? range = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(target);
        if (actor.MapInstanceId != target.MapInstanceId) return false;

        var allowed = range ?? pickupRange;
        if (!float.IsFinite(allowed) || allowed <= 0) return false;
        var delta = target.Position - actor.Position;
        return delta.LengthSquared <= allowed * allowed;
    }

    /// <summary>
    /// Reserva, valida e inserta el item. En éxito el WorldItem queda marcado PickedUp y debe
    /// retirarse del MapInstance por el caller dentro del mismo ciclo autoritativo.
    /// </summary>
    public InteractionResult TryPickup(Player player, WorldItem worldItem, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(worldItem);
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));

        if (!player.IsAlive)
            return InteractionResult.Fail(InteractionFailure.ActorUnavailable, "El jugador no puede interactuar en este estado.");
        if (player.MapInstanceId != worldItem.MapInstanceId)
            return InteractionResult.Fail(InteractionFailure.DifferentMap, "El item está en otra instancia de mapa.");
        if (!CanReach(player, worldItem))
            return InteractionResult.Fail(InteractionFailure.OutOfRange, "El item está fuera del alcance de interacción.");
        if (!worldItem.TryReserve(player.Id, nowMilliseconds, reservationMilliseconds))
            return InteractionResult.Fail(InteractionFailure.TargetUnavailable, "El item ya no está disponible.");

        if (!inventory.TryGive(player.Inventory, worldItem.Item, out var affected, out var error))
        {
            worldItem.ReleaseReservation(player.Id);
            return InteractionResult.Fail(InteractionFailure.InventoryRejected, error);
        }

        worldItem.MarkPickedUp(player.Id, nowMilliseconds);
        return InteractionResult.Ok(affected);
    }
}
