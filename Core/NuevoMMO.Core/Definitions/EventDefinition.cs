namespace NuevoMMO.Core;

/// <summary>
/// Definición autoritativa de un evento. Godot puede editarla y previsualizarla,
/// pero el servidor interpreta sus páginas, condiciones y comandos.
/// </summary>
public sealed record EventDefinition : GameDefinition
{
    public EventDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        EventScope scope = EventScope.Common,
        EventPlacementDefinition? placement = null,
        EventPageDefinition[]? pages = null,
        Dictionary<string, string>? metadata = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (scope == EventScope.Map && placement is null)
            throw new ArgumentException("Un EventDefinition de scope Map requiere Placement.", nameof(placement));
        if (scope != EventScope.Map && placement is not null)
            throw new ArgumentException("Placement solo es válido para eventos de mapa.", nameof(placement));

        var normalizedPages = pages?.ToArray() ?? [];
        if (normalizedPages.Select(static page => page.Id).Distinct().Count() != normalizedPages.Length)
            throw new ArgumentException("Pages contiene IDs duplicados.", nameof(pages));

        Scope = scope;
        Placement = placement;
        Pages = normalizedPages;
        Metadata = DefinitionCollectionGuards.CopyText(metadata, nameof(metadata));
    }

    public EventScope Scope { get; }
    public EventPlacementDefinition? Placement { get; }
    public EventPageDefinition[] Pages { get; }
    public Dictionary<string, string> Metadata { get; }
}
