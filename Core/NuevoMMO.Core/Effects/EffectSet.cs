namespace NuevoMMO.Core;

public sealed class EffectSet
{
    private readonly List<EffectInstance> effects = [];
    public IReadOnlyList<EffectInstance> All => effects;
    public void Add(EffectInstance effect) => effects.Add(effect);
    public bool Remove(DefinitionId definitionId) => effects.RemoveAll(effect => effect.DefinitionId == definitionId) > 0;
    public void Clear() => effects.Clear();
}
