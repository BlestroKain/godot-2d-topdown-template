using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;
using NuevoMMO.Server.World;

internal static class InventoryCircuitVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        VerifyPickupEquipStatsAndPersist();
    }

    private static void VerifyPickupEquipStatsAndPersist()
    {
        var sword = new ItemDefinition(
            DefinitionId.New(),
            new ContentKey("items.circuit_sword"),
            "Espada de circuito",
            string.Empty,
            true,
            1,
            null,
            new ContentKey("visuals.item.circuit_sword"),
            kind: ItemKind.Equipment,
            equipment: new ItemEquipmentDefinition(
                EquipmentSlot.Weapon,
                WeaponFamily.OneHanded,
                twoHanded: false,
                maxDurability: 100,
                flatStats: new Dictionary<StatId, float> { [StatId.Strength] = 5 }));

        var map = new MapDefinition(
            DefinitionId.New(), new ContentKey("maps.inventory_circuit"), "Circuito", string.Empty, true, 1, null,
            new ContentKey("maps.inventory_circuit.visual"),
            new BoundsData(new(0, 0), new(960, 640)), new Vector2Data(100, 100), new Vector2IntData(32, 32));
        var mob = new MobDefinition(
            DefinitionId.New(), new ContentKey("mobs.inventory_circuit"), "Mob", string.Empty, true, 1, null,
            new ContentKey("template.player"));
        var systems = new GameSystems(new DefinitionRegistry());
        systems.Definitions.Register(sword);
        var world = new WorldRuntime(map, mob, new WorldOptions(new(1), 120, 40, 50, 60, 32), new OscillatingMobPolicy(), new(900, 500), systems);
        var session = world.AddConnection(new ConnectionId(Guid.NewGuid()));
        world.AcceptProtocol(session);
        world.Authenticate(session, new AccountId(Guid.NewGuid()), new SessionId(Guid.NewGuid()), "token");
        world.Join(session, new CharacterSpawn(session.Account, new CharacterId(Guid.NewGuid()), "Heroe", map.Id, new Vector2Data(100, 100)));
        world.Activate(session, world.Instance);

        var player = session.Player ?? throw new InvalidOperationException("FAIL: jugador no unido");
        var natural = player.Stats.Primary.Strength;

        var ground = new WorldItem(
            new EntityId(10_001),
            new ItemInstance(new ItemInstanceId(Guid.NewGuid()), sword.Id, 1, 100),
            player.MapInstanceId,
            player.Position,
            sword.VisualKey,
            sword.Name);
        world.AddEntity(ground);

        var picked = world.Interact(session, ground.Id);
        Expect(picked.Success, "Interact recoge el item del suelo");
        Expect(player.Inventory.Count == 1, "El item entra al inventario autoritativo");

        var itemId = player.Inventory.Items[0].UniqueId;
        var message = world.EquipItem(session, itemId);
        Expect(message.Contains("Equipado", StringComparison.OrdinalIgnoreCase), "Equipar arma");
        Expect(player.Equipment.Contains(itemId), "El arma queda referenciada en equipo");
        Expect(player.Stats.Primary.Strength == natural + 5, "Equipar aplica STR del arma");

        var snapshot = InventoryProjection.Create(player);
        Expect(snapshot.Items.Length == 1 && snapshot.Equipped.Length == 1, "Snapshot incluye bolsa y equipo");

        var stored = CharacterInventoryStorage.FromPlayer(player);
        var restored = new Player(new EntityId(10_002), player.AccountId, player.CharacterId, player.MapInstanceId,
            player.Position, player.VisualKey, player.DisplayName);
        stored.ApplyTo(restored);
        Expect(restored.Inventory.Count == 1, "JSON de inventario restaura items");
        Expect(restored.Equipment.Contains(itemId), "JSON de inventario restaura equipo");

        world.UnequipItem(session, itemId);
        Expect(!player.Equipment.Contains(itemId), "Unequip libera el slot");
        Expect(player.Stats.Primary.Strength == natural, "Unequip quita el bono de STR");
    }

    private static void Expect(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("InventoryCircuitVerification FAIL: " + name);
    }
}
