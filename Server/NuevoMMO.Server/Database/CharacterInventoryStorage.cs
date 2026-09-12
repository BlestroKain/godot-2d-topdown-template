using System.Text.Json;
using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Database;

public sealed class CharacterInventoryStorage
{
    public const string EmptyJson = """{"items":[],"equipment":[]}""";

    public StoredInventoryItem[] Items { get; set; } = [];
    public StoredEquipmentEntry[] Equipment { get; set; } = [];

    public static CharacterInventoryStorage FromPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new CharacterInventoryStorage
        {
            Items = player.Inventory.Items.Select(item => new StoredInventoryItem(
                item.UniqueId.Value, item.DefinitionId.Value, item.Quantity, item.Durability)).ToArray(),
            Equipment = player.Equipment.Entries.Select(pair => new StoredEquipmentEntry(
                (byte)pair.Key.Slot, pair.Key.Index, pair.Value.Value)).ToArray()
        };
    }

    public void ApplyTo(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.Inventory.Clear();
        player.Equipment.Clear();
        foreach (var item in Items)
        {
            if (item.Id == Guid.Empty || item.Definition == Guid.Empty || item.Quantity < 1) continue;
            player.Inventory.Add(new ItemInstance(new(item.Id), new(item.Definition), item.Quantity, Math.Max(0, item.Durability)));
        }

        foreach (var entry in Equipment)
        {
            var slot = (EquipmentSlot)entry.Slot;
            if (slot == EquipmentSlot.None || !Enum.IsDefined(slot) || entry.ItemId == Guid.Empty) continue;
            if (!player.Inventory.Contains(new ItemInstanceId(entry.ItemId))) continue;
            player.Equipment.Equip(new EquipmentPosition(slot, entry.Index), new ItemInstanceId(entry.ItemId));
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static CharacterInventoryStorage Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new CharacterInventoryStorage();
        try
        {
            return JsonSerializer.Deserialize<CharacterInventoryStorage>(json) ?? new CharacterInventoryStorage();
        }
        catch (JsonException)
        {
            return new CharacterInventoryStorage();
        }
    }
}

public sealed record StoredInventoryItem(Guid Id, Guid Definition, int Quantity, int Durability);

public sealed record StoredEquipmentEntry(byte Slot, int Index, Guid ItemId);
