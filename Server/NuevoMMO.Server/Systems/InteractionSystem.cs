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
    InventoryRejected = 5,
    SystemUnavailable = 6
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

public readonly record struct HarvestInteractionResult(
    bool Success,
    InteractionFailure InteractionFailure,
    HarvestResult Harvest,
    string Message)
{
    public static HarvestInteractionResult Fail(InteractionFailure failure, string message)
        => new(false, failure, default, message);
}

/// <summary>
/// Puerta autoritativa para interacciones inmediatas. Valida estado, mapa y alcance antes de delegar
/// las reglas específicas al sistema dueño (inventario, recolección, etc.). No envía packets.
/// </summary>
public sealed class InteractionSystem
{
    private readonly InventorySystem inventory;
    private readonly ResourceHarvestSystem? harvesting;
    private readonly float interactionRange;
    private readonly int reservationMilliseconds;

    public InteractionSystem(
        InventorySystem inventory,
        float pickupRange = 64f,
        int reservationMilliseconds = 1_500,
        ResourceHarvestSystem? harvesting = null)
    {
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        if (!float.IsFinite(pickupRange) || pickupRange <= 0) throw new ArgumentOutOfRangeException(nameof(pickupRange));
        if (reservationMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(reservationMilliseconds));
        interactionRange = pickupRange;
        this.reservationMilliseconds = reservationMilliseconds;
        this.harvesting = harvesting;
    }

    public bool CanReach(Entity actor, Entity target, float? range = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(target);
        if (actor.MapInstanceId != target.MapInstanceId) return false;

        var allowed = range ?? interactionRange;
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

    public HarvestInteractionResult TryHarvest(
        Player player,
        ResourceEntity resource,
        float workPower,
        long nowMilliseconds,
        float? range = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(resource);
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));

        if (!player.IsAlive)
            return HarvestInteractionResult.Fail(InteractionFailure.ActorUnavailable, "El jugador no puede interactuar en este estado.");
        if (player.MapInstanceId != resource.MapInstanceId)
            return HarvestInteractionResult.Fail(InteractionFailure.DifferentMap, "El recurso está en otra instancia de mapa.");
        if (!CanReach(player, resource, range))
            return HarvestInteractionResult.Fail(InteractionFailure.OutOfRange, "El recurso está fuera del alcance de interacción.");
        if (harvesting is null)
            return HarvestInteractionResult.Fail(InteractionFailure.SystemUnavailable, "La recolección no está configurada en este runtime.");

        var result = harvesting.TryHarvest(player, resource, workPower, nowMilliseconds);
        return new(result.Success, InteractionFailure.None, result, result.Message);
    }
}
