namespace NuevoMMO.Core;

public sealed record ItemPropertyDefinition : GameDefinition
{
    public ItemPropertyDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version,
        string[]? tags, float minimum, float maximum) : base(id, key, name, description, enabled, version, tags)
    {
        if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum > maximum)
            throw new ArgumentException("Rango de propiedad inválido.");
        Minimum = minimum;
        Maximum = maximum;
    }

    public float Minimum { get; init; }
    public float Maximum { get; init; }
}
