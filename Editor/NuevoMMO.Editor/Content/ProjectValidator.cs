using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Valida el contenido abierto en el Editor usando las mismas reglas de ContentPackage
/// que se aplican antes de cargar contenido en runtime.
/// </summary>
public sealed class ProjectValidator
{
    private readonly DefinitionRegistry registry;

    public ProjectValidator(DefinitionRegistry registry)
        => this.registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public IReadOnlyList<string> Validate(string packageVersion = "editor")
        => BuildPackage(packageVersion).Validate();

    public void ValidateOrThrow(string packageVersion = "editor")
        => BuildPackage(packageVersion).ValidateOrThrow();

    public ContentPackage BuildPackage(string packageVersion = "editor")
    {
        if (string.IsNullOrWhiteSpace(packageVersion))
            throw new ArgumentException("PackageVersion requerido.", nameof(packageVersion));

        return new ContentPackage(
            ContentPackage.CurrentFormatVersion,
            packageVersion.Trim(),
            registry.GetAll<MapDefinition>().ToArray(),
            registry.GetAll<MobDefinition>().ToArray(),
            registry.GetAll<ItemDefinition>().ToArray(),
            registry.GetAll<EffectDefinition>().ToArray(),
            registry.GetAll<TechniqueDefinition>().ToArray(),
            registry.GetAll<NpcDefinition>().ToArray(),
            registry.GetAll<ResourceDefinition>().ToArray(),
            registry.GetAll<TraditionDefinition>().ToArray(),
            registry.GetAll<ProfessionDefinition>().ToArray(),
            registry.GetAll<RecipeDefinition>().ToArray(),
            registry.GetAll<LootTableDefinition>().ToArray(),
            registry.GetAll<SpawnTableDefinition>().ToArray(),
            registry.GetAll<DungeonDefinition>().ToArray(),
            registry.GetAll<QuestDefinition>().ToArray(),
            registry.GetAll<ItemPropertyDefinition>().ToArray())
        {
            Events = registry.GetAll<EventDefinition>().ToArray()
        };
    }
}
