namespace NuevoMMO.Core;

/// <summary>
/// Catálogo canónico de Tradiciones seleccionables al crear personaje.
/// Los nombres visibles siguen siendo provisionales de diseño; los DefinitionId son estables.
/// </summary>
public static class CanonicalTraditions
{
    public static readonly TraditionDefinition Veyrkan = Create(
        "52b44757-75dc-5999-97a2-65b683cde637", "tradition.veyrkan", "Veyrkan", "Pulso y Sobrecarga.");
    public static readonly TraditionDefinition Mizram = Create(
        "fbac221f-7ac0-5bc6-abbc-bd8e95fcf12b", "tradition.mizram", "Mizram", "Germinación y Red viva.");
    public static readonly TraditionDefinition Ngomei = Create(
        "dfbfd1ab-1eea-55f1-9d8b-93561a587612", "tradition.ngomei", "Ngomei", "Anclajes.");
    public static readonly TraditionDefinition Tegra = Create(
        "7da972ab-7722-5f05-a1ec-e7a58d708ff9", "tradition.tegra", "Tegra", "Trazado.");
    public static readonly TraditionDefinition Sagrel = Create(
        "e1017358-f2b5-5a41-a18c-2a8288f5ea58", "tradition.sagrel", "Sagrel", "Trayectoria.");
    public static readonly TraditionDefinition Kaelith = Create(
        "0fd1fc45-50eb-5dc5-9d14-46595ec745af", "tradition.kaelith", "Kaelith", "Resonancia elemental.");
    public static readonly TraditionDefinition Zoonai = Create(
        "f82ae869-b74b-5c40-805a-6dbacc16ecb8", "tradition.zoonai", "Zoonai", "Simbiosis con criaturas reales.");
    public static readonly TraditionDefinition Sahjin = Create(
        "69b47748-1dd6-580a-8e16-1bafa593af05", "tradition.sahjin", "Sahjin", "Cadencia corporal.");
    public static readonly TraditionDefinition Khemra = Create(
        "3e92631e-bf13-5de0-b66d-eeb8d8c6ff75", "tradition.khemra", "Khemra", "Circuitos y tecnología Malden.");
    public static readonly TraditionDefinition Nymbra = Create(
        "90d05d45-65f6-594c-b69c-d9d28c4da30b", "tradition.nymbra", "Nymbra", "Ecos y percepción.");

    public static IReadOnlyList<TraditionDefinition> All { get; } =
    [
        Veyrkan, Mizram, Ngomei, Tegra, Sagrel,
        Kaelith, Zoonai, Sahjin, Khemra, Nymbra
    ];

    private static readonly IReadOnlyDictionary<DefinitionId, TraditionDefinition> ById =
        All.ToDictionary(static tradition => tradition.Id);

    public static bool IsNovice(DefinitionId id) => id.IsEmpty;

    public static bool IsSelectable(DefinitionId id)
        => ById.TryGetValue(id, out var tradition) && tradition.Enabled;

    /// <summary>
    /// Create acepta Novicio (id vacío) o una Tradición publicada. Cualquier otro id se rechaza.
    /// </summary>
    public static bool IsValidAtCreate(DefinitionId id)
        => IsNovice(id) || IsSelectable(id);

    public static bool TryGet(DefinitionId id, out TraditionDefinition tradition)
        => ById.TryGetValue(id, out tradition!);

    public static string DisplayName(DefinitionId id)
        => IsNovice(id) ? "Novicio" : TryGet(id, out var tradition) ? tradition.Name : "Tradición desconocida";

    private static TraditionDefinition Create(string id, string key, string name, string description)
        => new(
            DefinitionId.Parse(id),
            new ContentKey(key),
            name,
            description,
            enabled: true,
            version: 1,
            tags: ["canonical", "tradition", "provisional-name"],
            visualKey: new ContentKey($"visuals.{key}"));
}
