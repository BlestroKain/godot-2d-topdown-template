using System.Text.Json;
using System.Text.Json.Serialization;

namespace NuevoMMO.Core;

public sealed record ContentPackage(
    int FormatVersion,
    string PackageVersion,
    MapDefinition[] Maps,
    MobDefinition[] Mobs,
    ItemDefinition[] Items,
    EffectDefinition[] Effects,
    TechniqueDefinition[] Techniques,
    NpcDefinition[] Npcs,
    ResourceDefinition[] Resources,
    TraditionDefinition[] Traditions,
    ProfessionDefinition[] Professions,
    RecipeDefinition[] Recipes,
    LootTableDefinition[] LootTables,
    SpawnTableDefinition[] SpawnTables,
    DungeonDefinition[] Dungeons,
    QuestDefinition[] Quests,
    ItemPropertyDefinition[] ItemProperties)
{
    public const int CurrentFormatVersion = 1;

    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    /// <summary>
    /// Enumera todas las Definitions contenidas en el paquete.
    /// </summary>
    public IEnumerable<GameDefinition> All()
    {
        foreach (var definition in Maps)
            yield return definition;

        foreach (var definition in Mobs)
            yield return definition;

        foreach (var definition in Items)
            yield return definition;

        foreach (var definition in Effects)
            yield return definition;

        foreach (var definition in Techniques)
            yield return definition;

        foreach (var definition in Npcs)
            yield return definition;

        foreach (var definition in Resources)
            yield return definition;

        foreach (var definition in Traditions)
            yield return definition;

        foreach (var definition in Professions)
            yield return definition;

        foreach (var definition in Recipes)
            yield return definition;

        foreach (var definition in LootTables)
            yield return definition;

        foreach (var definition in SpawnTables)
            yield return definition;

        foreach (var definition in Dungeons)
            yield return definition;

        foreach (var definition in Quests)
            yield return definition;

        foreach (var definition in ItemProperties)
            yield return definition;
    }

    /// <summary>
    /// Valida la integridad estructural básica del paquete.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (FormatVersion != CurrentFormatVersion)
        {
            errors.Add(
                $"Formato de contenido no soportado. " +
                $"Recibido: {FormatVersion}. Esperado: {CurrentFormatVersion}.");
        }

        if (string.IsNullOrWhiteSpace(PackageVersion))
            errors.Add("PackageVersion requerido.");

        var ids = new HashSet<DefinitionId>();
        var keys = new HashSet<ContentKey>();

        foreach (var definition in All())
        {
            if (definition.Id.IsEmpty)
            {
                errors.Add(
                    $"Definition con ID vacío: {definition.Key}.");
            }
            else if (!ids.Add(definition.Id))
            {
                errors.Add(
                    $"DefinitionId duplicado: {definition.Id}.");
            }

            if (!keys.Add(definition.Key))
            {
                errors.Add(
                    $"ContentKey duplicado: {definition.Key}.");
            }
        }

        return errors;
    }

    /// <summary>
    /// Valida el paquete y lanza una excepción si contiene errores.
    /// </summary>
    public void ValidateOrThrow()
    {
        var errors = Validate();

        if (errors.Count == 0)
            return;

        throw new InvalidDataException(
            "ContentPackage inválido:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, errors.Select(
                static error => $" - {error}")));
    }

    /// <summary>
    /// Carga el paquete en un DefinitionRegistry.
    ///
    /// Primero realiza una carga completa sobre un Registry temporal
    /// para evitar destruir el Registry actual si el paquete falla.
    /// </summary>
    public void LoadInto(DefinitionRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        ValidateOrThrow();

        // Primera pasada: comprobar que todas las Definitions
        // pueden registrarse correctamente.
        var temporaryRegistry = new DefinitionRegistry();

        RegisterAllInto(temporaryRegistry);

        // Solo modificamos el Registry real cuando toda la carga
        // anterior ha terminado correctamente.
        registry.Clear();

        RegisterAllInto(registry);
    }

    /// <summary>
    /// Serializa el paquete a JSON.
    /// </summary>
    public string ToJson()
        => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Deserializa un ContentPackage desde JSON.
    /// </summary>
    public static ContentPackage FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException(
                "El JSON del ContentPackage está vacío.",
                nameof(json));

        try
        {
            return JsonSerializer.Deserialize<ContentPackage>(
                       json,
                       JsonOptions)
                   ?? throw new InvalidDataException(
                       "ContentPackage vacío.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "No se pudo deserializar el ContentPackage.",
                exception);
        }
    }

    /// <summary>
    /// Crea un paquete vacío utilizando el formato actual.
    /// </summary>
    public static ContentPackage Empty(string packageVersion)
    {
        if (string.IsNullOrWhiteSpace(packageVersion))
            throw new ArgumentException(
                "PackageVersion requerido.",
                nameof(packageVersion));

        return new ContentPackage(
            CurrentFormatVersion,
            packageVersion.Trim(),
            Maps: [],
            Mobs: [],
            Items: [],
            Effects: [],
            Techniques: [],
            Npcs: [],
            Resources: [],
            Traditions: [],
            Professions: [],
            Recipes: [],
            LootTables: [],
            SpawnTables: [],
            Dungeons: [],
            Quests: [],
            ItemProperties: []);
    }

    private void RegisterAllInto(DefinitionRegistry registry)
    {
        foreach (var definition in Maps)
            registry.Register(definition);

        foreach (var definition in Mobs)
            registry.Register(definition);

        foreach (var definition in Items)
            registry.Register(definition);

        foreach (var definition in Effects)
            registry.Register(definition);

        foreach (var definition in Techniques)
            registry.Register(definition);

        foreach (var definition in Npcs)
            registry.Register(definition);

        foreach (var definition in Resources)
            registry.Register(definition);

        foreach (var definition in Traditions)
            registry.Register(definition);

        foreach (var definition in Professions)
            registry.Register(definition);

        foreach (var definition in Recipes)
            registry.Register(definition);

        foreach (var definition in LootTables)
            registry.Register(definition);

        foreach (var definition in SpawnTables)
            registry.Register(definition);

        foreach (var definition in Dungeons)
            registry.Register(definition);

        foreach (var definition in Quests)
            registry.Register(definition);

        foreach (var definition in ItemProperties)
            registry.Register(definition);
    }
}