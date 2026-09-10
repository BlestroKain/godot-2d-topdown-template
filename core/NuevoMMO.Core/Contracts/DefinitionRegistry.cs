namespace NuevoMMO.Core;

public sealed class DefinitionRegistry
{
    private readonly Dictionary<Type, Dictionary<DefinitionId, GameDefinition>> byType = [];
    private readonly Dictionary<ContentKey, GameDefinition> byKey = [];

    public void Register<T>(T definition) where T : GameDefinition
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!byType.TryGetValue(typeof(T), out var bucket)) byType[typeof(T)] = bucket = [];
        if (!bucket.TryAdd(definition.Id, definition)) throw new InvalidOperationException("DefinitionId duplicado.");
        if (!byKey.TryAdd(definition.Key, definition)) throw new InvalidOperationException("ContentKey duplicado.");
    }

    public T Get<T>(DefinitionId id) where T : GameDefinition
        => TryGet<T>(id, out var definition) ? definition! : throw new KeyNotFoundException("Definition inexistente.");

    public bool TryGet<T>(DefinitionId id, out T? definition) where T : GameDefinition
    {
        definition = default;
        if (!byType.TryGetValue(typeof(T), out var bucket) || !bucket.TryGetValue(id, out var value) || value is not T typed)
            return false;
        definition = typed;
        return true;
    }

    public IReadOnlyList<T> GetAll<T>() where T : GameDefinition
        => byType.TryGetValue(typeof(T), out var bucket) ? bucket.Values.OfType<T>().ToArray() : [];

    public bool Unregister<T>(DefinitionId id) where T : GameDefinition
    {
        if (!byType.TryGetValue(typeof(T), out var bucket) || !bucket.Remove(id, out var definition)) return false;
        byKey.Remove(definition.Key);
        return true;
    }

    public void Reload<T>(IEnumerable<T> definitions) where T : GameDefinition
    {
        if (byType.TryGetValue(typeof(T), out var bucket))
        {
            foreach (var existing in bucket.Values) byKey.Remove(existing.Key);
            byType.Remove(typeof(T));
        }
        foreach (var definition in definitions) Register(definition);
    }

    public void Clear()
    {
        byType.Clear();
        byKey.Clear();
    }
}
