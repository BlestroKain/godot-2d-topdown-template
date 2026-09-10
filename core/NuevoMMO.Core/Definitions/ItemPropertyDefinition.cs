namespace NuevoMMO.Core;

/// <summary>
/// Describe una propiedad que puede existir en objetos o recursos.
/// El rango global funciona como referencia; cada ItemDefinition/ResourceDefinition puede sobrescribirlo.
/// </summary>
public sealed record ItemPropertyDefinition : GameDefinition
{
    public ItemPropertyDefinition(
        DefinitionId id,
        ContentKey key,
        string name,
        string? description,
        bool enabled,
        int version,
        string[]? tags,
        float minimum,
        float maximum,
        ModifierType modifierType = ModifierType.Flat,
        StatId? statId = null,
        bool appliesToItems = true,
        bool appliesToResources = false,
        string? unit = null,
        int displayPrecision = 0,
        Dictionary<string, string>? metadata = null)
        : base(id, key, name, description, enabled, version, tags)
    {
        DefaultRange = new NumericRange(minimum, maximum);
        if (!appliesToItems && !appliesToResources)
            throw new ArgumentException("La propiedad debe aplicar al menos a objetos o recursos.");
        if (displayPrecision is < 0 or > 8)
            throw new ArgumentOutOfRangeException(nameof(displayPrecision));

        ModifierType = modifierType;
        StatId = statId;
        AppliesToItems = appliesToItems;
        AppliesToResources = appliesToResources;
        Unit = unit?.Trim() ?? string.Empty;
        DisplayPrecision = displayPrecision;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public float Minimum => DefaultRange.Minimum;
    public float Maximum => DefaultRange.Maximum;
    public NumericRange DefaultRange { get; }
    public ModifierType ModifierType { get; }

    /// <summary>
    /// Stat canónico afectado cuando la propiedad representa directamente un stat.
    /// Puede ser null para propiedades de oficio/material como Pureza o Conductividad Malden.
    /// </summary>
    public StatId? StatId { get; }

    public bool AppliesToItems { get; }
    public bool AppliesToResources { get; }
    public string Unit { get; }
    public int DisplayPrecision { get; }
    public Dictionary<string, string> Metadata { get; }
}
