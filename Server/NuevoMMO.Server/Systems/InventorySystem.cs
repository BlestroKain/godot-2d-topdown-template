using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

/// <summary>
/// Reglas autoritativas de inventario. Adapta el flujo CanGive/Give/Take de Intersect,
/// pero mantiene el estado en Inventory y resuelve las reglas desde ItemDefinition.
/// </summary>
public sealed class InventorySystem
{
    private readonly DefinitionRegistry definitions;

    public InventorySystem(DefinitionRegistry definitions)
        => this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));

    public bool CanGive(Inventory inventory, ItemInstance incoming, out string error)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(incoming);
        error = string.Empty;

        if (incoming.Quantity < 1)
        {
            error = "Cantidad inválida.";
            return false;
        }
        if (inventory.Contains(incoming.UniqueId))
        {
            error = "La instancia de item ya existe en el inventario.";
            return false;
        }
        if (!definitions.TryGet<ItemDefinition>(incoming.DefinitionId, out var definition) || definition is null || !definition.Enabled)
        {
            error = "ItemDefinition inexistente o deshabilitada.";
            return false;
        }

        var remaining = (long)incoming.Quantity;
        if (definition.Stacking.Stackable)
        {
            foreach (var stack in inventory.Find(incoming.DefinitionId))
            {
                if (!CanStack(stack, incoming)) continue;
                remaining -= Math.Max(0, definition.Stacking.MaxInventoryStack - stack.Quantity);
                if (remaining <= 0) return true;
            }
        }

        var perNewSlot = definition.Stacking.Stackable ? definition.Stacking.MaxInventoryStack : 1;
        var requiredSlots = (remaining + perNewSlot - 1) / perNewSlot;
        if (requiredSlots > inventory.FreeSlots)
        {
            error = "Inventario lleno.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Inserta una instancia de forma atómica. Si la cantidad excede un stack, crea nuevas instancias
    /// conservando properties/metadata; la primera pila nueva conserva el UniqueId recibido.
    /// </summary>
    public IReadOnlyList<ItemInstanceId> Give(Inventory inventory, ItemInstance incoming)
    {
        if (!CanGive(inventory, incoming, out var error)) throw new InvalidOperationException(error);
        var definition = definitions.Get<ItemDefinition>(incoming.DefinitionId);
        var affected = new List<ItemInstanceId>();
        var remaining = incoming.Quantity;

        if (definition.Stacking.Stackable)
        {
            foreach (var stack in inventory.Find(incoming.DefinitionId))
            {
                if (!CanStack(stack, incoming)) continue;
                var free = definition.Stacking.MaxInventoryStack - stack.Quantity;
                if (free <= 0) continue;
                var moved = Math.Min(free, remaining);
                stack.Quantity += moved;
                remaining -= moved;
                affected.Add(stack.UniqueId);
                if (remaining == 0) return affected;
            }
        }

        var maxStack = definition.Stacking.Stackable ? definition.Stacking.MaxInventoryStack : 1;
        var useIncomingId = true;
        while (remaining > 0)
        {
            var quantity = Math.Min(maxStack, remaining);
            var id = useIncomingId ? incoming.UniqueId : new ItemInstanceId(Guid.NewGuid());
            useIncomingId = false;
            var stack = Clone(incoming, id, quantity);
            inventory.Add(stack);
            affected.Add(stack.UniqueId);
            remaining -= quantity;
        }

        return affected;
    }

    public bool TryGive(Inventory inventory, ItemInstance incoming, out IReadOnlyList<ItemInstanceId> affected, out string error)
    {
        affected = [];
        if (!CanGive(inventory, incoming, out error)) return false;
        affected = Give(inventory, incoming);
        return true;
    }

    public bool CanTake(Inventory inventory, DefinitionId definitionId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        if (definitionId.IsEmpty) return false;
        if (quantity < 1) return false;
        return inventory.QuantityOf(definitionId) >= quantity;
    }

    /// <summary>
    /// Consume por DefinitionId empezando por las pilas más pequeñas para liberar slots antes.
    /// No modifica nada si la cantidad total no alcanza.
    /// </summary>
    public IReadOnlyList<ItemInstanceId> Take(Inventory inventory, DefinitionId definitionId, int quantity)
    {
        if (!CanTake(inventory, definitionId, quantity))
            throw new InvalidOperationException("Cantidad insuficiente del item solicitado.");

        var affected = new List<ItemInstanceId>();
        var remaining = quantity;
        foreach (var stack in inventory.Find(definitionId).OrderBy(static item => item.Quantity).ToArray())
        {
            if (remaining == 0) break;
            affected.Add(stack.UniqueId);
            if (stack.Quantity <= remaining)
            {
                remaining -= stack.Quantity;
                inventory.Remove(stack.UniqueId);
            }
            else
            {
                stack.Quantity -= remaining;
                remaining = 0;
            }
        }
        return affected;
    }

    public bool CanTake(Inventory inventory, ItemInstanceId itemId, int quantity)
        => quantity > 0 && inventory.TryGet(itemId, out var item) && item is not null && item.Quantity >= quantity;

    public void Take(Inventory inventory, ItemInstanceId itemId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        if (!CanTake(inventory, itemId, quantity))
            throw new InvalidOperationException("Cantidad insuficiente en la instancia solicitada.");

        var item = inventory.Get(itemId);
        if (item.Quantity == quantity) inventory.Remove(itemId);
        else item.Quantity -= quantity;
    }

    public static bool CanStack(ItemInstance left, ItemInstance right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.DefinitionId != right.DefinitionId || left.Durability != right.Durability) return false;
        if (left.Properties.Length != right.Properties.Length || left.Metadata.Count != right.Metadata.Count) return false;

        var leftProperties = left.Properties.OrderBy(static value => value.PropertyId.Value).ToArray();
        var rightProperties = right.Properties.OrderBy(static value => value.PropertyId.Value).ToArray();
        if (!leftProperties.SequenceEqual(rightProperties)) return false;

        foreach (var pair in left.Metadata)
            if (!right.Metadata.TryGetValue(pair.Key, out var value) || !string.Equals(pair.Value, value, StringComparison.Ordinal))
                return false;
        return true;
    }

    private static ItemInstance Clone(ItemInstance source, ItemInstanceId id, int quantity)
        => new(id, source.DefinitionId, quantity, source.Durability, source.Properties, source.Metadata);
}
