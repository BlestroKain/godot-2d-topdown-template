using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class ClientEntityManager
{
    private readonly Dictionary<EntityId, ClientEntity> entities = [];
    public IReadOnlyDictionary<EntityId, ClientEntity> All => entities;

    public ClientEntity Spawn(EntityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var entity = ClientEntityFactory.FromState(state);
        entities[state.Id] = entity;
        return entity;
    }

    public bool Despawn(EntityId id) => entities.Remove(id);

    public void UpdateState(EntityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (entities.TryGetValue(state.Id, out var entity) && entity.Kind == state.Kind)
        {
            entity.State = state;
            return;
        }

        Spawn(state);
    }

    public bool TryGet(EntityId id, out ClientEntity? entity) => entities.TryGetValue(id, out entity);

    public ClientEntity Get(EntityId id)
        => entities.TryGetValue(id, out var entity) ? entity : throw new KeyNotFoundException("Entidad cliente inexistente.");

    public T Get<T>(EntityId id) where T : ClientEntity
        => Get(id) as T ?? throw new InvalidCastException($"La entidad {id} no es {typeof(T).Name}.");

    public void SyncFromCache(EntityStateCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        foreach (var id in entities.Keys.Where(id => !cache.Contains(id)).ToArray())
            Despawn(id);
        foreach (var state in cache.All.Values)
            UpdateState(state);
    }

    public void ClearMap() => entities.Clear();
}
