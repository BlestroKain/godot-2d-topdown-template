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

        var account = new AccountId(Guid.NewGuid());
        var characters = new InMemoryCharacterRepository();
        var record = characters.CreateAsync(
            account,
            "Heroe",
            map.Id,
            new Vector2Data(100, 100),
            DefinitionId.Empty).GetAwaiter().GetResult();
        var persistence = new PersistenceService(characters, map, _ => map.Id);

        var world = new WorldRuntime(map, mob, new WorldOptions(new(1), 120, 40, 50, 60, 32), new OscillatingMobPolicy(), new(900, 500), systems);
        var session = world.AddConnection(new ConnectionId(Guid.NewGuid()));
        world.AcceptProtocol(session);
        world.Authenticate(session, account, new SessionId(Guid.NewGuid()), "token");
        world.Join(session, new CharacterSpawn(account, record.Id, record.Name, map.Id, record.Position));
        world.Activate(session, world.Instance);

        var player = session.Player ?? throw new InvalidOperationException("FAIL: jugador no unido");
        systems.Progression.Initialize(player, record.ToProgressionState());
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

        persistence.SaveCharacterAsync(player).GetAwaiter().GetResult();
        var persisted = characters.GetAsync(record.Id).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("InventoryCircuitVerification FAIL: personaje no persistido");
        var loaded = persistence.LoadCharacterAsync(account, persisted).GetAwaiter().GetResult();

        var restored = new Player(
            new EntityId(10_002),
            account,
            record.Id,
            player.MapInstanceId,
            persisted.Position,
            player.VisualKey,
            persisted.Name);
        systems.Progression.Initialize(restored, loaded.Progression, preserveVitals: false);
        persistence.RestoreInventory(restored, persisted);
        systems.Equipment.RepairInvalidEquipment(restored);
        systems.Progression.Recalculate(restored, preserveVitals: loaded.CurrentHealth is not null);

        Expect(restored.Inventory.Count == 1, "Save/load restaura items tras reconexión");
        Expect(restored.Equipment.Contains(itemId), "Save/load restaura equipo tras reconexión");
        Expect(restored.Stats.Primary.Strength == natural + 5, "Reconexión recalcula stats desde equipo restaurado");
        Expect(CharacterInventoryStorage.FromPlayer(restored).ToJson() == persisted.InventoryData,
            "Estado restaurado coincide con snapshot persistido");

        world.UnequipItem(session, itemId);
        Expect(!player.Equipment.Contains(itemId), "Unequip libera el slot");
        Expect(player.Stats.Primary.Strength == natural, "Unequip quita el bono de STR");
    }

    private static void Expect(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("InventoryCircuitVerification FAIL: " + name);
    }
}
