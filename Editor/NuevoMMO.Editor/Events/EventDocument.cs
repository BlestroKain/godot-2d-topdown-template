using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Copia mutable de trabajo de EventDefinition. Pages siguen siendo valores completos e inmutables;
/// el editor reemplaza páginas de forma transaccional y al guardar genera una nueva Definition.
/// </summary>
public sealed class EventDocument
{
    private EventDocument() { }

    public DefinitionId Id { get; private set; }
    public ContentKey Key { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int Version { get; set; }
    public List<string> Tags { get; } = [];
    public EventScope Scope { get; set; }
    public EventPlacementDefinition? Placement { get; set; }
    public List<EventPageDefinition> Pages { get; } = [];
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static EventDocument FromDefinition(EventDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var document = new EventDocument
        {
            Id = definition.Id,
            Key = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            Enabled = definition.Enabled,
            Version = definition.Version,
            Scope = definition.Scope,
            Placement = definition.Placement
        };
        document.Tags.AddRange(definition.Tags);
        document.Pages.AddRange(definition.Pages);
        foreach (var pair in definition.Metadata) document.Metadata.Add(pair.Key, pair.Value);
        return document;
    }

    public EventDefinition ToDefinition()
        => new(
            Id,
            Key,
            Name,
            Description,
            Enabled,
            Version,
            Tags.ToArray(),
            Scope,
            Placement,
            Pages.ToArray(),
            new Dictionary<string, string>(Metadata, StringComparer.OrdinalIgnoreCase));
}
