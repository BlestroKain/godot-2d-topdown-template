using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Validador único del proyecto de contenido. Construye el mismo ContentPackage que consume
/// el servidor, evitando que el Editor y runtime tengan reglas de integridad distintas.
/// </summary>
public sealed class ProjectValidator
{
    private readonly DefinitionRegistry registry;

    public ProjectValidator(DefinitionRegistry registry)
        => this.registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public ContentPackage BuildPackage(string packageVersion)
    {
        if (string.IsNullOrWhiteSpace(packageVersion))
            throw new ArgumentException("PackageVersion requerido.", nameof(packageVersion));

        return new ContentPackage(
            ContentPackage.CurrentFormatVersion,
            packageVersion.Trim(),
            Maps: registry.GetAll<MapDefinition>().ToArray(),
            Mobs: registry.GetAll<MobDefinition>().ToArray(),
            Items: registry.GetAll<ItemDefinition>().ToArray(),
            Effects: registry.GetAll<EffectDefinition>().ToArray(),
            Techniques: registry.GetAll<TechniqueDefinition>().ToArray(),
            Npcs: registry.GetAll<NpcDefinition>().ToArray(),
            Resources: registry.GetAll<ResourceDefinition>().ToArray(),
            Traditions: registry.GetAll<TraditionDefinition>().ToArray(),
            Professions: registry.GetAll<ProfessionDefinition>().ToArray(),
            Recipes: registry.GetAll<RecipeDefinition>().ToArray(),
            LootTables: registry.GetAll<LootTableDefinition>().ToArray(),
            SpawnTables: registry.GetAll<SpawnTableDefinition>().ToArray(),
            Dungeons: registry.GetAll<DungeonDefinition>().ToArray(),
            Quests: registry.GetAll<QuestDefinition>().ToArray(),
            ItemProperties: registry.GetAll<ItemPropertyDefinition>().ToArray())
        {
            Events = registry.GetAll<EventDefinition>().ToArray()
        };
    }

    public IReadOnlyList<string> Validate(string packageVersion = "editor")
        => BuildPackage(packageVersion).Validate();
}
