using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public abstract class DefinitionEditor<T> where T : GameDefinition
{
    protected DefinitionEditor(DefinitionRegistry registry) => Registry = registry;
    protected DefinitionRegistry Registry { get; }
    public IReadOnlyList<T> List() => Registry.GetAll<T>();
    public void Create(T definition) { Registry.Register(definition); }
    public T Edit(DefinitionId id) => Registry.Get<T>(id);
    public bool Delete(DefinitionId id) => Registry.Unregister<T>(id);
    public IReadOnlyList<T> Search(string query) => List().Where(definition =>
        definition.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        definition.Key.Value.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
    public IReadOnlyList<T> Filter(Func<T, bool> predicate) => List().Where(predicate).ToArray();
}
