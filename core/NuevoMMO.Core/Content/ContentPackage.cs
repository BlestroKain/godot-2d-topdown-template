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
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new ContentKeyJsonConverter(),
            new DefinitionIdJsonConverter()
        }
    };

    public IEnumerable<GameDefinition> All()
    {
        foreach (var definition in Maps) yield return definition;
        foreach (var definition in Mobs) yield return definition;
        foreach (var definition in Items) yield return definition;
        foreach (var definition in Effects) yield return definition;
        foreach (var definition in Techniques) yield return definition;
        foreach (var definition in Npcs) yield return definition;
        foreach (var definition in Resources) yield return definition;
        foreach (var definition in Traditions) yield return definition;
        foreach (var definition in Professions) yield return definition;
        foreach (var definition in Recipes) yield return definition;
        foreach (var definition in LootTables) yield return definition;
        foreach (var definition in SpawnTables) yield return definition;
        foreach (var definition in Dungeons) yield return definition;
        foreach (var definition in Quests) yield return definition;
        foreach (var definition in ItemProperties) yield return definition;
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (FormatVersion != CurrentFormatVersion)
            errors.Add($"Formato de contenido no soportado. Recibido: {FormatVersion}. Esperado: {CurrentFormatVersion}.");
        if (string.IsNullOrWhiteSpace(PackageVersion)) errors.Add("PackageVersion requerido.");

        var ids = new HashSet<DefinitionId>();
        var keys = new HashSet<ContentKey>();
        foreach (var definition in All())
        {
            if (definition.Id.IsEmpty) errors.Add($"Definition con ID vacío: {definition.Key}.");
            else if (!ids.Add(definition.Id)) errors.Add($"DefinitionId duplicado: {definition.Id}.");
            if (!keys.Add(definition.Key)) errors.Add($"ContentKey duplicado: {definition.Key}.");
        }

        ValidateReferences(errors);
        return errors;
    }

    private void ValidateReferences(List<string> errors)
    {
        var itemIds = Items.Select(static value => value.Id).ToHashSet();
        var propertyIds = ItemProperties.Select(static value => value.Id).ToHashSet();
        var lootIds = LootTables.Select(static value => value.Id).ToHashSet();
        var techniqueIds = Techniques.Select(static value => value.Id).ToHashSet();
        var effectIds = Effects.Select(static value => value.Id).ToHashSet();
        var professionIds = Professions.Select(static value => value.Id).ToHashSet();

        foreach (var item in Items)
        {
            foreach (var propertyId in item.PropertyIds)
                Require(propertyIds, propertyId, item.Key, "ItemPropertyDefinition", errors);
            if (item.Use?.TechniqueId is { } techniqueId)
                Require(techniqueIds, techniqueId, item.Key, "TechniqueDefinition", errors);
        }

        foreach (var lootTable in LootTables)
        {
            foreach (var itemId in lootTable.ItemIds)
                Require(itemIds, itemId, lootTable.Key, "ItemDefinition", errors);
            foreach (var entry in lootTable.Entries)
                foreach (var propertyId in entry.PropertyOverrides.Keys)
                    Require(propertyIds, propertyId, lootTable.Key, "ItemPropertyDefinition", errors);
        }

        foreach (var resource in Resources)
        {
            foreach (var propertyId in resource.PropertyIds)
                Require(propertyIds, propertyId, resource.Key, "ItemPropertyDefinition", errors);
            if (resource.Harvest.LootTableId is { } lootId)
                Require(lootIds, lootId, resource.Key, "LootTableDefinition", errors);
            if (resource.Harvest.RequiredProfessionId is { } professionId)
                Require(professionIds, professionId, resource.Key, "ProfessionDefinition", errors);
        }

        foreach (var mob in Mobs)
        {
            if (mob.LootTableId is { } lootId) Require(lootIds, lootId, mob.Key, "LootTableDefinition", errors);
            ValidateCombatReferences(mob.Key, mob.Combat, techniqueIds, effectIds, errors);
        }

        foreach (var npc in Npcs)
        {
            if (npc.LootTableId is { } lootId) Require(lootIds, lootId, npc.Key, "LootTableDefinition", errors);
            if (npc.Combat is { } combat)
                ValidateCombatReferences(npc.Key, combat, techniqueIds, effectIds, errors);
        }
    }

    private static void ValidateCombatReferences(
        ContentKey owner,
        CreatureCombatDefinition combat,
        HashSet<DefinitionId> techniqueIds,
        HashSet<DefinitionId> effectIds,
        List<string> errors)
    {
        foreach (var techniqueId in combat.TechniqueIds)
            Require(techniqueIds, techniqueId, owner, "TechniqueDefinition", errors);
        foreach (var effectId in combat.ImmunityEffectIds)
            Require(effectIds, effectId, owner, "EffectDefinition", errors);
    }

    private static void Require(
        HashSet<DefinitionId> available,
        DefinitionId referenced,
        ContentKey owner,
        string expectedType,
        List<string> errors)
    {
        if (!available.Contains(referenced))
            errors.Add($"{owner} referencia {expectedType} inexistente: {referenced}.");
    }

    public void ValidateOrThrow()
    {
        var errors = Validate();
        if (errors.Count == 0) return;
        throw new InvalidDataException(
            "ContentPackage inválido:" + Environment.NewLine +
            string.Join(Environment.NewLine, errors.Select(static error => $" - {error}")));
    }

    public void LoadInto(DefinitionRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ValidateOrThrow();
        var temporaryRegistry = new DefinitionRegistry();
        RegisterAllInto(temporaryRegistry);
        registry.Clear();
        RegisterAllInto(registry);
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static ContentPackage FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("El JSON del ContentPackage está vacío.", nameof(json));
        try
        {
            return JsonSerializer.Deserialize<ContentPackage>(json, JsonOptions)
                ?? throw new InvalidDataException("ContentPackage vacío.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("No se pudo deserializar el ContentPackage.", exception);
        }
    }

    public static ContentPackage Empty(string packageVersion)
    {
        if (string.IsNullOrWhiteSpace(packageVersion))
            throw new ArgumentException("PackageVersion requerido.", nameof(packageVersion));
        return new ContentPackage(
            CurrentFormatVersion, packageVersion.Trim(), Maps: [], Mobs: [], Items: [], Effects: [], Techniques: [],
            Npcs: [], Resources: [], Traditions: [], Professions: [], Recipes: [], LootTables: [], SpawnTables: [],
            Dungeons: [], Quests: [], ItemProperties: []);
    }

    private void RegisterAllInto(DefinitionRegistry registry)
    {
        foreach (var definition in All()) registry.Register(definition);
    }
}
