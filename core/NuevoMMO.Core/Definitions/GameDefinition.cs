namespace NuevoMMO.Core;

public abstract record GameDefinition
{
    protected GameDefinition(DefinitionId id, ContentKey key, string name, string description, bool enabled, int version, string[]? tags)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("DefinitionId vacío.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nombre de presentación vacío.", nameof(name));
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        Id = id;
        Key = key;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Enabled = enabled;
        Version = version;
        Tags = tags?.Distinct(StringComparer.Ordinal).ToArray() ?? [];
    }

    public DefinitionId Id { get; init; }
    public ContentKey Key { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public bool Enabled { get; init; }
    public int Version { get; init; }
    public string[] Tags { get; init; }
}
