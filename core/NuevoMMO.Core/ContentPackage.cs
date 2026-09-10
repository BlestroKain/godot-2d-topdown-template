using System.Text.Json;
using System.Text.Json.Serialization;

namespace NuevoMMO.Core;

public sealed record ContentPackage(int FormatVersion, string PackageVersion, MapDefinition[] Maps,
    MobDefinition[] Mobs, ItemDefinition[] Items)
{
    public const int CurrentFormatVersion = 1;
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (FormatVersion != CurrentFormatVersion) errors.Add($"Formato no soportado: {FormatVersion}.");
        if (string.IsNullOrWhiteSpace(PackageVersion)) errors.Add("PackageVersion requerido.");
        var ids = new HashSet<DefinitionId>();
        var keys = new HashSet<ContentKey>();
        foreach (var definition in Maps.Cast<GameDefinition>().Concat(Mobs).Concat(Items))
        {
            if (!ids.Add(definition.Id)) errors.Add($"DefinitionId duplicado: {definition.Id.Value}.");
            if (!keys.Add(definition.Key)) errors.Add($"ContentKey duplicado: {definition.Key.Value}.");
        }
        return errors;
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
    public static ContentPackage FromJson(string json) => JsonSerializer.Deserialize<ContentPackage>(json, JsonOptions)
        ?? throw new InvalidDataException("ContentPackage vacío.");
}
