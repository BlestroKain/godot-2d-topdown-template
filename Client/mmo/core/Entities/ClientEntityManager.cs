using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class ClientEntity
{
    public required EntityId Id { get; init; }
    public required EntityState State { get; set; }
}

public sealed class ClientEntityManager
{
    private readonly Dictionary<EntityId, ClientEntity> entities = [];
    public IReadOnlyDictionary<EntityId, ClientEntity> All => entities;

    public ClientEntity Spawn(EntityState state)
    {
        var entity = new ClientEntity { Id = state.Id, State = state };
        entities[state.Id] = entity;
        return entity;
    }

    public bool Despawn(EntityId id) => entities.Remove(id);
    public void UpdateState(EntityState state)
    {
        if (entities.TryGetValue(state.Id, out var entity)) entity.State = state;
        else Spawn(state);
    }
    public ClientEntity Get(EntityId id) => entities[id];
    public void ClearMap() => entities.Clear();
}
