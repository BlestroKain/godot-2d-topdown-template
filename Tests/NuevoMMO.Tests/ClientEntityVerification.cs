using System.Runtime.CompilerServices;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;

internal static class ClientEntityVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        var mob = new MobState(new EntityId(2), DefinitionId.New(), new MapInstanceId(1), new Vector2Data(10, 0),
            default, Direction.Down, new ContentKey("template.player"), "Lobo");
        var player = new PlayerState(new EntityId(1), new CharacterId(Guid.NewGuid()), new MapInstanceId(1),
            new Vector2Data(0, 0), default, Direction.Down, new ContentKey("template.player"), "Heroe");
        var resource = new ResourceState(new EntityId(3), DefinitionId.New(), new MapInstanceId(1), new Vector2Data(20, 0),
            Direction.Down, new ContentKey("template.player"), "Mena");

        var manager = new ClientEntityManager();
        manager.Spawn(player);
        manager.Spawn(mob);
        manager.Spawn(resource);
        Expect(manager.Get<ClientPlayer>(player.Id).TryTarget(mob.Id), "IClientPlayer.TryTarget");
        Expect(manager.Get<ClientMob>(mob.Id).MobDefinition == mob.MobDefinition, "ClientMob conserva Definition");
        Expect(manager.Get<ClientResource>(resource.Id).ResourceDefinition == resource.ResourceDefinition, "IClientResource");

        var cache = new EntityStateCache();
        cache.Upsert(player);
        cache.Upsert(mob);
        manager.SyncFromCache(cache);
        Expect(!manager.All.ContainsKey(resource.Id), "SyncFromCache despawnea lo que salió del snapshot");

        var hotbar = new HotbarState();
        hotbar.Bind(0, HotbarBindingKind.Technique, DefinitionId.New());
        Expect(!hotbar[0].IsEmpty && hotbar.Slots.Count == 6, "Hotbar de 6 slots como el HUD BR");
        hotbar.Swap(0, 1);
        Expect(hotbar[0].IsEmpty && !hotbar[1].IsEmpty, "Hotbar.Swap intercambia bindings");

        var item = new ClientItem(new ItemInstanceId(Guid.NewGuid()), DefinitionId.New(), 3);
        Expect(item.Quantity == 3, "IClientItem cantidad");

        var chat = new ChatState();
        chat.Append(ChatChannel.Combat, "Golpe");
        Expect(chat.Messages.Count == 1, "ChatState local");

        var map = new ClientMap(new MapProjection(DefinitionId.New(), new MapInstanceId(1), new ContentKey("maps.x"),
            new BoundsData(new(0, 0), new(64, 64)), 120, 50, "dev"));
        map.AddAction(new Vector2Data(1, 1), "+10");
        Expect(map.ActionMessages.Count == 1, "IClientMap action messages");

        var state = new ClientGameState();
        state.Start(new MapLoadPacket(map.Projection, player.Id, player.Character));
        Expect(state.Flow == GameFlowState.Loading && state.ClientMap is not null, "GameFlow Loading al MapLoad");
        state.Apply(new EntityStatePacket(1, true, new MovementCorrection(player.Id, player.Position, 0), [player], []), 0);
        Expect(state.Flow == GameFlowState.InWorld && state.WorldEntities.Get<ClientPlayer>(player.Id).Id == player.Id,
            "WorldEntities se sincroniza con el snapshot");
        state.Clear();
        Expect(state.Flow == GameFlowState.Disconnected, "Clear vuelve a Disconnected");
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("ClientEntityVerification FAIL: " + name);
    }
}
