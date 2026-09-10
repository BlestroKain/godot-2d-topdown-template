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
        foreach (var definition in All())
        {
            if (!ids.Add(definition.Id)) errors.Add($"DefinitionId duplicado: {definition.Id.Value}.");
            if (!keys.Add(definition.Key)) errors.Add($"ContentKey duplicado: {definition.Key.Value}.");
        }
        return errors;
    }

    public IEnumerable<GameDefinition> All() => Maps.Cast<GameDefinition>()
        .Concat(Mobs).Concat(Items).Concat(Effects).Concat(Techniques).Concat(Npcs).Concat(Resources)
        .Concat(Traditions).Concat(Professions).Concat(Recipes).Concat(LootTables).Concat(SpawnTables)
        .Concat(Dungeons).Concat(Quests).Concat(ItemProperties);

    public void LoadInto(DefinitionRegistry registry)
    {
        foreach (var error in Validate()) throw new InvalidDataException(error);
        registry.Clear();
        foreach (var map in Maps) registry.Register(map);
        foreach (var mob in Mobs) registry.Register(mob);
        foreach (var item in Items) registry.Register(item);
        foreach (var effect in Effects) registry.Register(effect);
        foreach (var technique in Techniques) registry.Register(technique);
        foreach (var npc in Npcs) registry.Register(npc);
        foreach (var resource in Resources) registry.Register(resource);
        foreach (var tradition in Traditions) registry.Register(tradition);
        foreach (var profession in Professions) registry.Register(profession);
        foreach (var recipe in Recipes) registry.Register(recipe);
        foreach (var loot in LootTables) registry.Register(loot);
        foreach (var spawn in SpawnTables) registry.Register(spawn);
        foreach (var dungeon in Dungeons) registry.Register(dungeon);
        foreach (var quest in Quests) registry.Register(quest);
        foreach (var property in ItemProperties) registry.Register(property);
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static ContentPackage FromJson(string json) => JsonSerializer.Deserialize<ContentPackage>(json, JsonOptions)
        ?? throw new InvalidDataException("ContentPackage vacío.");

    public static ContentPackage Empty(string packageVersion) => new(CurrentFormatVersion, packageVersion, [], [], [],
        [], [], [], [], [], [], [], [], [], [], [], []);
}
