namespace NuevoMMO.Core;

public sealed class DefinitionRegistry
{
    private readonly Dictionary<Type, Dictionary<DefinitionId, GameDefinition>> byType = [];
    private readonly Dictionary<DefinitionId, GameDefinition> byId = [];
    private readonly Dictionary<ContentKey, GameDefinition> byKey = [];

    public int Count => byId.Count;

    public void Register<T>(T definition)
        where T : GameDefinition
    {
        ArgumentNullException.ThrowIfNull(definition);

        var type = typeof(T);

        if (byId.ContainsKey(definition.Id))
            throw new InvalidOperationException(
                $"DefinitionId duplicado: {definition.Id}.");

        if (byKey.ContainsKey(definition.Key))
            throw new InvalidOperationException(
                $"ContentKey duplicado: {definition.Key}.");

        if (!byType.TryGetValue(type, out var bucket))
        {
            bucket = [];
            byType[type] = bucket;
        }

        bucket.Add(definition.Id, definition);
        byId.Add(definition.Id, definition);
        byKey.Add(definition.Key, definition);
    }

    public T Get<T>(DefinitionId id)
        where T : GameDefinition
    {
        if (TryGet<T>(id, out var definition))
            return definition!;

        throw new KeyNotFoundException(
            $"No existe una Definition de tipo {typeof(T).Name} con ID {id}.");
    }

    public bool TryGet<T>(
        DefinitionId id,
        out T? definition)
        where T : GameDefinition
    {
        definition = default;

        if (!byId.TryGetValue(id, out var value))
            return false;

        if (value is not T typed)
            return false;

        definition = typed;
        return true;
    }

    public GameDefinition Get(ContentKey key)
    {
        if (byKey.TryGetValue(key, out var definition))
            return definition;

        throw new KeyNotFoundException(
            $"No existe una Definition con ContentKey '{key}'.");
    }

    public bool TryGet(
        ContentKey key,
        out GameDefinition? definition)
    {
        return byKey.TryGetValue(key, out definition);
    }

    public T Get<T>(ContentKey key)
        where T : GameDefinition
    {
        if (TryGet<T>(key, out var definition))
            return definition!;
        throw new KeyNotFoundException(
            $"No existe una Definition de tipo {typeof(T).Name} con ContentKey '{key}'.");
    }

    public bool TryGet<T>(
        ContentKey key,
        out T? definition)
        where T : GameDefinition
    {
        definition = default;

        if (!byKey.TryGetValue(key, out var value))
            return false;

        if (value is not T typed)
            return false;

        definition = typed;
        return true;
    }

    public IReadOnlyList<T> GetAll<T>()
        where T : GameDefinition
    {
        return byId.Values
            .OfType<T>()
            .ToArray();
    }

    public bool Contains(DefinitionId id)
        => byId.ContainsKey(id);

    public bool Contains(ContentKey key)
        => byKey.ContainsKey(key);

    public bool Unregister(DefinitionId id)
    {
        if (!byId.Remove(id, out var definition))
            return false;

        byKey.Remove(definition.Key);

        var type = definition.GetType();

        if (byType.TryGetValue(type, out var bucket))
        {
            bucket.Remove(id);

            if (bucket.Count == 0)
                byType.Remove(type);
        }

        return true;
    }

    public void Reload<T>(IEnumerable<T> definitions)
        where T : GameDefinition
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var incoming = definitions.ToArray();

        ValidateReload(incoming);

        var existingIds = byId.Values
            .OfType<T>()
            .Select(static definition => definition.Id)
            .ToArray();

        foreach (var id in existingIds)
            Unregister(id);

        foreach (var definition in incoming)
            Register(definition);
    }

    public void Clear()
    {
        byType.Clear();
        byId.Clear();
        byKey.Clear();
    }

    private void ValidateReload<T>(IReadOnlyCollection<T> definitions)
        where T : GameDefinition
    {
        var duplicateId = definitions
            .GroupBy(static definition => definition.Id)
            .FirstOrDefault(static group => group.Count() > 1);

        if (duplicateId is not null)
            throw new InvalidOperationException(
                $"DefinitionId duplicado durante Reload: {duplicateId.Key}.");

        var duplicateKey = definitions
            .GroupBy(static definition => definition.Key)
            .FirstOrDefault(static group => group.Count() > 1);

        if (duplicateKey is not null)
            throw new InvalidOperationException(
                $"ContentKey duplicado durante Reload: {duplicateKey.Key}.");

        var replacingIds = byId.Values
            .OfType<T>()
            .Select(static definition => definition.Id)
            .ToHashSet();

        var replacingKeys = byId.Values
            .OfType<T>()
            .Select(static definition => definition.Key)
            .ToHashSet();

        foreach (var definition in definitions)
        {
            if (byId.ContainsKey(definition.Id) &&
                !replacingIds.Contains(definition.Id))
            {
                throw new InvalidOperationException(
                    $"DefinitionId {definition.Id} ya pertenece a otra Definition.");
            }

            if (byKey.ContainsKey(definition.Key) &&
                !replacingKeys.Contains(definition.Key))
            {
                throw new InvalidOperationException(
                    $"ContentKey '{definition.Key}' ya pertenece a otra Definition.");
            }
        }
    }
}