using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public sealed record EquipmentChange(
    EquipmentPosition Position,
    ItemInstanceId Equipped,
    ItemInstanceId? Replaced,
    IReadOnlyList<ItemInstanceId> AutomaticallyUnequipped);

/// <summary>
/// Reglas autoritativas de equipamiento. Mantiene la idea de Intersect de que el equipo
/// referencia instancias del inventario, pero usa EquipmentSlot tipado y resuelve aquí
/// requisitos/conflictos en lugar de convertir Player en una clase monolítica.
/// </summary>
public sealed class EquipmentSystem
{
    private readonly DefinitionRegistry definitions;

    public EquipmentSystem(DefinitionRegistry definitions)
        => this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));

    public bool CanEquip(Player player, ItemInstanceId itemId, EquipmentPosition position, out string error)
    {
        ArgumentNullException.ThrowIfNull(player);
        error = string.Empty;

        if (!player.Inventory.TryGet(itemId, out var item) || item is null)
        {
            error = "El item no está en el inventario.";
            return false;
        }
        if (!definitions.TryGet<ItemDefinition>(item.DefinitionId, out var definition) || definition is null || !definition.Enabled)
        {
            error = "ItemDefinition inexistente o deshabilitada.";
            return false;
        }
        if (definition.Kind != ItemKind.Equipment || definition.Equipment is null)
        {
            error = "El item no es equipable.";
            return false;
        }
        if (definition.Equipment.Slot != position.Slot)
        {
            error = $"El item requiere el slot {definition.Equipment.Slot}.";
            return false;
        }
        if (!IsIndexedSlot(position.Slot) && position.Index != 0)
        {
            error = $"El slot {position.Slot} no admite índice {position.Index}.";
            return false;
        }
        if (definition.Equipment.MaxDurability is not null && item.Durability <= 0)
        {
            error = "El equipo está roto.";
            return false;
        }

        foreach (var requirement in definition.Requirements.MinimumStats)
        {
            if (ReadStat(player.Stats, requirement.Key) >= requirement.Value) continue;
            error = string.IsNullOrWhiteSpace(definition.Requirements.CannotUseMessage)
                ? $"No cumple el requisito de {requirement.Key}: {requirement.Value}."
                : definition.Requirements.CannotUseMessage;
            return false;
        }

        return true;
    }

    public EquipmentChange Equip(Player player, ItemInstanceId itemId, EquipmentPosition position)
    {
        if (!CanEquip(player, itemId, position, out var error)) throw new InvalidOperationException(error);
        var item = player.Inventory.Get(itemId);
        var definition = definitions.Get<ItemDefinition>(item.DefinitionId);
        var automaticallyUnequipped = new List<ItemInstanceId>();

        // Mover una misma instancia de una posición indexada a otra es válido.
        if (player.Equipment.Unequip(itemId, out _))
        {
            // No se reporta como auto-unequip: sigue siendo el mismo item que se está equipando.
        }

        if (position.Slot == EquipmentSlot.Weapon && definition.Equipment!.TwoHanded)
            UnequipAll(player, EquipmentSlot.OffHand, automaticallyUnequipped);
        else if (position.Slot == EquipmentSlot.OffHand)
            UnequipTwoHandedWeapon(player, automaticallyUnequipped);

        var replaced = player.Equipment.Equip(position, itemId);
        return new EquipmentChange(position, itemId, replaced, automaticallyUnequipped);
    }

    public bool Unequip(Player player, EquipmentPosition position, out ItemInstanceId itemId)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.Equipment.Unequip(position, out itemId);
    }

    public bool Unequip(Player player, ItemInstanceId itemId, out EquipmentPosition position)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.Equipment.Unequip(itemId, out position);
    }

    public IReadOnlyList<ItemInstance> EquippedItems(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var result = new List<ItemInstance>();
        foreach (var itemId in player.Equipment.Entries.Values.Distinct())
            if (player.Inventory.TryGet(itemId, out var item) && item is not null) result.Add(item);
        return result;
    }

    public Dictionary<StatId, float> FlatStatBonuses(Player player)
        => SumEquipmentStats(player, static equipment => equipment.FlatStats);

    public Dictionary<StatId, float> PercentStatBonuses(Player player)
        => SumEquipmentStats(player, static equipment => equipment.PercentStats);

    /// <summary>
    /// Elimina referencias de equipo huérfanas o incompatibles, útil después de cargar/migrar un personaje.
    /// </summary>
    public IReadOnlyList<ItemInstanceId> RepairInvalidEquipment(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var removed = new List<ItemInstanceId>();
        foreach (var pair in player.Equipment.Entries.ToArray())
        {
            if (IsValidEquippedEntry(player, pair.Key, pair.Value)) continue;
            if (player.Equipment.Unequip(pair.Key, out var itemId)) removed.Add(itemId);
        }
        return removed;
    }

    private bool IsValidEquippedEntry(Player player, EquipmentPosition position, ItemInstanceId itemId)
    {
        if (!player.Inventory.TryGet(itemId, out var item) || item is null) return false;
        if (!definitions.TryGet<ItemDefinition>(item.DefinitionId, out var definition) || definition?.Equipment is null) return false;
        return definition.Enabled && definition.Kind == ItemKind.Equipment && definition.Equipment.Slot == position.Slot;
    }

    private void UnequipTwoHandedWeapon(Player player, ICollection<ItemInstanceId> removed)
    {
        if (!player.Equipment.TryGet(EquipmentSlot.Weapon, out var weaponId)) return;
        if (!player.Inventory.TryGet(weaponId, out var weapon) || weapon is null) return;
        if (!definitions.TryGet<ItemDefinition>(weapon.DefinitionId, out var definition) || definition?.Equipment?.TwoHanded != true) return;
        if (player.Equipment.Unequip(new EquipmentPosition(EquipmentSlot.Weapon), out var unequipped)) removed.Add(unequipped);
    }

    private static void UnequipAll(Player player, EquipmentSlot slot, ICollection<ItemInstanceId> removed)
    {
        foreach (var position in player.Equipment.PositionsFor(slot).ToArray())
            if (player.Equipment.Unequip(position, out var itemId)) removed.Add(itemId);
    }

    private Dictionary<StatId, float> SumEquipmentStats(
        Player player,
        Func<ItemEquipmentDefinition, IReadOnlyDictionary<StatId, float>> selector)
    {
        var result = new Dictionary<StatId, float>();
        foreach (var item in EquippedItems(player))
        {
            if (!definitions.TryGet<ItemDefinition>(item.DefinitionId, out var definition) || definition?.Equipment is null) continue;
            foreach (var pair in selector(definition.Equipment))
                result[pair.Key] = result.GetValueOrDefault(pair.Key) + pair.Value;
        }
        return result;
    }

    private static bool IsIndexedSlot(EquipmentSlot slot)
        => slot is EquipmentSlot.Ring or EquipmentSlot.Trophy;

    private static float ReadStat(StatBlock stats, StatId stat)
        => stat switch
        {
            StatId.Strength => stats.Primary.Strength,
            StatId.Intelligence => stats.Primary.Intelligence,
            StatId.Agility => stats.Primary.Agility,
            StatId.Spirit => stats.Primary.Spirit,
            StatId.Vitality => stats.Primary.Vitality,
            StatId.Luck => stats.Secondary.Luck,
            StatId.ResistEarth => stats.Resistances.Earth,
            StatId.ResistFire => stats.Resistances.Fire,
            StatId.ResistAir => stats.Resistances.Air,
            StatId.ResistWater => stats.Resistances.Water,
            StatId.ResistNeutral => stats.Resistances.Neutral,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };
}
