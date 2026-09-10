using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class DefinitionEditorDescriptor
{
    private readonly Func<EditorApplication, DefinitionId, ContentKey, string, GameDefinition> factory;

    public DefinitionEditorDescriptor(
        Type definitionType,
        string title,
        string singularName,
        string keyPrefix,
        Func<EditorApplication, DefinitionId, ContentKey, string, GameDefinition> factory)
    {
        DefinitionType = definitionType ?? throw new ArgumentNullException(nameof(definitionType));
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Título requerido.", nameof(title)) : title;
        SingularName = string.IsNullOrWhiteSpace(singularName)
            ? throw new ArgumentException("Nombre singular requerido.", nameof(singularName))
            : singularName;
        KeyPrefix = string.IsNullOrWhiteSpace(keyPrefix)
            ? throw new ArgumentException("Prefijo requerido.", nameof(keyPrefix))
            : keyPrefix;
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public Type DefinitionType { get; }
    public string Title { get; }
    public string SingularName { get; }
    public string KeyPrefix { get; }

    public GameDefinition Create(EditorApplication application)
    {
        var identity = NextIdentity(application);
        return factory(application, identity.Id, identity.Key, identity.Name);
    }

    public (DefinitionId Id, ContentKey Key, string Name) NextIdentity(EditorApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        var number = application.Content.Snapshot().All().Count(DefinitionType.IsInstanceOfType) + 1;
        ContentKey key;
        do key = new ContentKey($"{KeyPrefix}_{number++:000}");
        while (application.Definitions.Contains(key));

        return (DefinitionId.New(), key, $"{SingularName} {number - 1}");
    }
}

public static class DefinitionEditorDescriptors
{
    public static DefinitionEditorDescriptor Items { get; } = new(
        typeof(ItemDefinition), "Items", "Item", "items.item",
        static (_, id, key, name) => new ItemDefinition(
            id, key, name, string.Empty, true, 1, ["item"], new ContentKey("visuals.item.placeholder")));

    public static DefinitionEditorDescriptor Mobs { get; } = new(
        typeof(MobDefinition), "Mobs", "Mob", "mobs.mob",
        static (_, id, key, name) => new MobDefinition(
            id, key, name, string.Empty, true, 1, ["mob"], new ContentKey("visuals.mob.placeholder")));

    public static DefinitionEditorDescriptor Npcs { get; } = new(
        typeof(NpcDefinition), "NPCs", "NPC", "npcs.npc",
        static (_, id, key, name) => new NpcDefinition(
            id, key, name, string.Empty, true, 1, ["npc"], new ContentKey("visuals.npc.placeholder")));

    public static DefinitionEditorDescriptor Resources { get; } = new(
        typeof(ResourceDefinition), "Recursos", "Recurso", "resources.resource",
        static (_, id, key, name) => new ResourceDefinition(
            id, key, name, string.Empty, true, 1, ["resource"], new ContentKey("visuals.resource.placeholder")));

    public static DefinitionEditorDescriptor Techniques { get; } = new(
        typeof(TechniqueDefinition), "Técnicas / Spells", "Técnica", "techniques.technique",
        static (_, id, key, name) => new TechniqueDefinition(
            id, key, name, string.Empty, true, 1, ["technique"], new ContentKey("visuals.technique.placeholder")));

    public static DefinitionEditorDescriptor Effects { get; } = new(
        typeof(EffectDefinition), "Efectos", "Efecto", "effects.effect",
        static (_, id, key, name) => new EffectDefinition(
            id, key, name, string.Empty, true, 1, ["effect"], new ContentKey("visuals.effect.placeholder")));

    public static DefinitionEditorDescriptor Traditions { get; } = new(
        typeof(TraditionDefinition), "Tradiciones / Clases", "Tradición", "traditions.tradition",
        static (_, id, key, name) => new TraditionDefinition(id, key, name, string.Empty, true, 1, ["tradition"]));

    public static DefinitionEditorDescriptor Professions { get; } = new(
        typeof(ProfessionDefinition), "Profesiones", "Profesión", "professions.profession",
        static (_, id, key, name) => new ProfessionDefinition(id, key, name, string.Empty, true, 1, ["profession"]));

    public static DefinitionEditorDescriptor Recipes { get; } = new(
        typeof(RecipeDefinition), "Recetas", "Receta", "recipes.recipe",
        static (app, id, key, name) =>
        {
            var profession = app.Definitions.GetAll<ProfessionDefinition>().FirstOrDefault()
                ?? throw new InvalidOperationException("Cree una profesión antes de crear una receta.");
            var item = app.Definitions.GetAll<ItemDefinition>().FirstOrDefault()
                ?? throw new InvalidOperationException("Cree un item antes de crear una receta.");
            return new RecipeDefinition(
                id, key, name, string.Empty, true, 1, ["recipe"], profession.Id, [], item.Id);
        });

    public static DefinitionEditorDescriptor LootTables { get; } = new(
        typeof(LootTableDefinition), "Loot Tables", "Loot Table", "loot_tables.loot_table",
        static (_, id, key, name) => new LootTableDefinition(id, key, name, string.Empty, true, 1, ["loot"], []));

    public static DefinitionEditorDescriptor SpawnTables { get; } = new(
        typeof(SpawnTableDefinition), "Spawn Tables", "Spawn Table", "spawn_tables.spawn_table",
        static (_, id, key, name) => new SpawnTableDefinition(id, key, name, string.Empty, true, 1, ["spawn"], []));

    public static DefinitionEditorDescriptor Quests { get; } = new(
        typeof(QuestDefinition), "Quests", "Quest", "quests.quest",
        static (_, id, key, name) => new QuestDefinition(id, key, name, string.Empty, true, 1, ["quest"]));

    public static DefinitionEditorDescriptor Events { get; } = new(
        typeof(EventDefinition), "Eventos", "Evento", "events.event",
        static (_, id, key, name) => new EventDefinition(id, key, name, string.Empty, true, 1, ["event"]));

    public static DefinitionEditorDescriptor Dungeons { get; } = new(
        typeof(DungeonDefinition), "Dungeons", "Dungeon", "dungeons.dungeon",
        static (app, id, key, name) =>
        {
            var map = app.Definitions.GetAll<MapDefinition>().FirstOrDefault()
                ?? throw new InvalidOperationException("Cree un mapa antes de crear un dungeon.");
            return new DungeonDefinition(id, key, name, string.Empty, true, 1, ["dungeon"], map.Id);
        });

    public static DefinitionEditorDescriptor ItemProperties { get; } = new(
        typeof(ItemPropertyDefinition), "Propiedades de item", "Propiedad", "item_properties.property",
        static (_, id, key, name) => new ItemPropertyDefinition(
            id, key, name, string.Empty, true, 1, ["item-property"], 0, 0));

    public static DefinitionEditorDescriptor Tilesets { get; } = new(
        typeof(TilesetDefinition), "Tilesets", "Tileset", "tilesets.tileset",
        static (_, id, key, name) => new TilesetDefinition(
            id, key, name, string.Empty, true, 1, ["tileset"], new ContentKey("tilesets.placeholder"), new(32, 32)));

    public static IReadOnlyList<DefinitionEditorDescriptor> All { get; } =
    [
        Items, Mobs, Npcs, Resources, Techniques, Effects, Traditions, Professions,
        Recipes, LootTables, SpawnTables, Quests, Events, Dungeons, ItemProperties, Tilesets
    ];
}
