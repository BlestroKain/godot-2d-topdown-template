using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class EntityStateCache
{
    private readonly Dictionary<EntityId, EntityState> entities = [];
    public IReadOnlyDictionary<EntityId, EntityState> All => entities;
    public void Upsert(EntityState state) => entities[state.Id] = state;
    public void Remove(EntityId id) => entities.Remove(id);
    public void Clear() => entities.Clear();
    public bool Contains(EntityId id) => entities.ContainsKey(id);
}
