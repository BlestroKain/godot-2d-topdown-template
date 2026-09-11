namespace NuevoMMO.Core;

/// <summary>
/// Definición maestra de un recurso recolectable del mundo.
/// La entidad runtime conserva estado actual; esta Definition contiene su configuración editable.
/// </summary>
public sealed record ResourceDefinition : GameDefinition
{
    public ResourceDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        ContentKey visualKey,
        DefinitionId[]? propertyIds = null,
        ContentKey? exhaustedVisualKey = null,
        ResourceHarvestDefinition? harvest = null,
        Dictionary<DefinitionId, NumericRange>? propertyRanges = null,
        Dictionary<string, DefinitionId>? eventHooks = null,
        Dictionary<string, string>? metadata = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        if (visualKey.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));
        if (exhaustedVisualKey is { } exhausted && exhausted.IsEmpty)
            throw new ArgumentException("ExhaustedVisualKey vacío.", nameof(exhaustedVisualKey));

        var ids = DefinitionModelGuards.CopyIds(propertyIds, nameof(propertyIds));
        PropertyRanges = DefinitionModelGuards.CopyRanges(propertyRanges, nameof(propertyRanges));

        VisualKey = visualKey;
        ExhaustedVisualKey = exhaustedVisualKey;
        Harvest = harvest ?? new ResourceHarvestDefinition();
        EventHooks = DefinitionModelGuards.CopyDefinitionHooks(eventHooks, nameof(eventHooks));
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        PropertyIds = ids.Concat(PropertyRanges.Keys).Distinct().ToArray();
    }

    public ContentKey VisualKey { get; }
    public ContentKey? ExhaustedVisualKey { get; }

    /// <summary>
    /// Configuración de recolección: loot, profesión/herramienta, salud, respawn y bloqueo de movimiento.
    /// </summary>
    public ResourceHarvestDefinition Harvest { get; }

    public DefinitionId[] PropertyIds { get; }
    public Dictionary<DefinitionId, NumericRange> PropertyRanges { get; }

    /// <summary>
    /// Hooks editables como onHarvest, onExhausted u otros futuros eventos de contenido.
    /// Las claves son semánticas y el valor es la Definition objetivo.
    /// </summary>
    public Dictionary<string, DefinitionId> EventHooks { get; }
    public Dictionary<string, string> Metadata { get; }
}
