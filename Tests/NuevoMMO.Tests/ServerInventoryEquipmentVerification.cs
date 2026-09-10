using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;

internal static class ServerInventoryEquipmentVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyInventoryStacking();
        VerifyEquipmentConflicts();
        VerifyEquipmentRequirements();
    }

    private static void VerifyInventoryStacking()
    {
        var registry = new DefinitionRegistry();
        var material = new ItemDefinition(
            DefinitionId.New(),
            new ContentKey("items.material_test"),
            "Material Test",
            string.Empty,
            true,
            1,
            null,
            new ContentKey("visuals.material_test"),
            kind: ItemKind.Material,
            stacking: new ItemStackDefinition(stackable: true, maxInventoryStack: 5, maxBankStack: 20));
        registry.Register(material);

        var inventory = new Inventory(maxSlots: 2);
        var system = new InventorySystem(registry);
        var first = new ItemInstance(new ItemInstanceId(Guid.NewGuid()), material.Id, 7, 0);
        var changed = system.Give(inventory, first);

        Expect(changed.Count == 2, "7 unidades generan dos stacks");
        Expect(inventory.Count == 2, "inventario usa dos slots");
        Expect(inventory.Items.Select(static item => item.Quantity).Order().SequenceEqual([2, 5]), "stacks respetan máximo 5");

        var second = new ItemInstance(new ItemInstanceId(Guid.NewGuid()), material.Id, 3, 0);
        Expect(system.CanGive(inventory, second, out _), "stack existente acepta tres unidades");
        system.Give(inventory, second);
        Expect(inventory.Items.All(static item => item.Quantity == 5), "stack parcial se completa antes de crear slot");

        var overflow = new ItemInstance(new ItemInstanceId(Guid.NewGuid()), material.Id, 1, 0);
        Expect(!system.CanGive(inventory, overflow, out _), "inventario lleno rechaza overflow de forma atómica");
        Expect(inventory.QuantityOf(material.Id) == 10, "rechazo no altera cantidades");

        system.Take(inventory, material.Id, 6);
        Expect(inventory.QuantityOf(material.Id) == 4, "Take por DefinitionId consume cantidad exacta");
        Expect(inventory.Count == 1, "Take libera stacks completos primero");
    }

    private static void VerifyEquipmentConflicts()
    {
        var registry = new DefinitionRegistry();
        var weaponDefinition = EquipmentDefinition("items.weapon_twohand_test", EquipmentSlot.Weapon, twoHanded: true);
        var offHandDefinition = EquipmentDefinition("items.offhand_test", EquipmentSlot.OffHand);
        registry.Register(weaponDefinition);
        registry.Register(offHandDefinition);

        var player = CreatePlayer();
        var inventorySystem = new InventorySystem(registry);
        var equipmentSystem = new EquipmentSystem(registry);
        var weapon = new ItemInstance(new ItemInstanceId(Guid.NewGuid()), weaponDefinition.Id, 1, 100);
        var offHand = new ItemInstance(new ItemInstanceId(Guid.NewGuid()), offHandDefinition.Id, 1, 100);
        inventorySystem.Give(player.Inventory, weapon);
        inventorySystem.Give(player.Inventory, offHand);

        equipmentSystem.Equip(player, offHand.UniqueId, new EquipmentPosition(EquipmentSlot.OffHand));
        var weaponChange = equipmentSystem.Equip(player, weapon.UniqueId, new EquipmentPosition(EquipmentSlot.Weapon));
        Expect(weaponChange.AutomaticallyUnequipped.Contains(offHand.UniqueId), "arma a dos manos retira off-hand");
        Expect(!player.Equipment.TryGet(EquipmentSlot.OffHand, out _), "off-hand queda libre");

        var offHandChange = equipmentSystem.Equip(player, offHand.UniqueId, new EquipmentPosition(EquipmentSlot.OffHand));
        Expect(offHandChange.AutomaticallyUnequipped.Contains(weapon.UniqueId), "equipar off-hand retira arma a dos manos");
        Expect(!player.Equipment.TryGet(EquipmentSlot.Weapon, out _), "weapon queda libre");
    }

    private static void VerifyEquipmentRequirements()
    {
        var registry = new DefinitionRegistry();
        var required = new ItemDefinition(
            DefinitionId.New(),
            new ContentKey("items.strength_ring_test"),
            "Strength Ring",
            string.Empty,
            true,
            1,
            null,
            new ContentKey("visuals.strength_ring_test"),
            kind: ItemKind.Equipment,
            equipment: new ItemEquipmentDefinition(EquipmentSlot.Ring, maxDurability: 50),
            requirements: new ItemRequirementDefinition(new Dictionary<StatId, float> { [StatId.Strength] = 10 }));
        registry.Register(required);

        var player = CreatePlayer();
        var inventorySystem = new InventorySystem(registry);
        var equipmentSystem = new EquipmentSystem(registry);
        var ring = new ItemInstance(new ItemInstanceId(Guid.NewGuid()), required.Id, 1, 50);
        inventorySystem.Give(player.Inventory, ring);

        player.Stats.Primary.Strength = 9;
        Expect(!equipmentSystem.CanEquip(player, ring.UniqueId, new EquipmentPosition(EquipmentSlot.Ring, 1), out _),
            "requisito de stat bloquea equipamiento");
        player.Stats.Primary.Strength = 10;
        Expect(equipmentSystem.CanEquip(player, ring.UniqueId, new EquipmentPosition(EquipmentSlot.Ring, 1), out _),
            "requisito exacto permite equipamiento");
    }

    private static ItemDefinition EquipmentDefinition(string key, EquipmentSlot slot, bool twoHanded = false)
        => new(
            DefinitionId.New(),
            new ContentKey(key),
            key,
            string.Empty,
            true,
            1,
            null,
            new ContentKey(key.Replace("items.", "visuals.", StringComparison.Ordinal)),
            kind: ItemKind.Equipment,
            equipment: new ItemEquipmentDefinition(slot, twoHanded: twoHanded, maxDurability: 100));

    private static Player CreatePlayer()
        => new(
            new EntityId(1),
            new AccountId(Guid.NewGuid()),
            new CharacterId(Guid.NewGuid()),
            new MapInstanceId(1),
            default,
            new ContentKey("visuals.player_test"),
            "Player Test");

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("ServerInventoryEquipmentVerification FAIL: " + name);
    }
}
