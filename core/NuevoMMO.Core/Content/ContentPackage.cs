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

    /// <summary>
    /// Eventos comunes/globales y eventos colocados en mapas. Se mantiene fuera del constructor
    /// posicional para conservar compatibilidad con paquetes v1 anteriores.
    /// </summary>
    public EventDefinition[] Events { get; init; } = [];

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
        foreach (var definition in Maps ?? []) yield return definition;
        foreach (var definition in Mobs ?? []) yield return definition;
        foreach (var definition in Items ?? []) yield return definition;
        foreach (var definition in Effects ?? []) yield return definition;
        foreach (var definition in Techniques ?? []) yield return definition;
        foreach (var definition in Npcs ?? []) yield return definition;
        foreach (var definition in Resources ?? []) yield return definition;
        foreach (var definition in Traditions ?? []) yield return definition;
        foreach (var definition in Professions ?? []) yield return definition;
        foreach (var definition in Recipes ?? []) yield return definition;
        foreach (var definition in LootTables ?? []) yield return definition;
        foreach (var definition in SpawnTables ?? []) yield return definition;
        foreach (var definition in Dungeons ?? []) yield return definition;
        foreach (var definition in Quests ?? []) yield return definition;
        foreach (var definition in ItemProperties ?? []) yield return definition;
        foreach (var definition in Events ?? []) yield return definition;
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

        ValidateReferences(errors, ids);
        return errors;
    }

    private void ValidateReferences(List<string> errors, HashSet<DefinitionId> allIds)
    {
        var mapIds = (Maps ?? []).Select(static value => value.Id).ToHashSet();
        var mobIds = (Mobs ?? []).Select(static value => value.Id).ToHashSet();
        var npcIds = (Npcs ?? []).Select(static value => value.Id).ToHashSet();
        var resourceIds = (Resources ?? []).Select(static value => value.Id).ToHashSet();
        var itemIds = (Items ?? []).Select(static value => value.Id).ToHashSet();
        var propertyIds = (ItemProperties ?? []).Select(static value => value.Id).ToHashSet();
        var lootIds = (LootTables ?? []).Select(static value => value.Id).ToHashSet();
        var techniqueIds = (Techniques ?? []).Select(static value => value.Id).ToHashSet();
        var effectIds = (Effects ?? []).Select(static value => value.Id).ToHashSet();
        var professionIds = (Professions ?? []).Select(static value => value.Id).ToHashSet();
        var recipeIds = (Recipes ?? []).Select(static value => value.Id).ToHashSet();
        var spawnTableIds = (SpawnTables ?? []).Select(static value => value.Id).ToHashSet();
        var eventIds = (Events ?? []).Select(static value => value.Id).ToHashSet();
        var spawnableIds = mobIds.Concat(npcIds).Concat(resourceIds).ToHashSet();

        foreach (var item in Items ?? [])
        {
            foreach (var propertyId in item.PropertyIds) Require(propertyIds, propertyId, item.Key, "ItemPropertyDefinition", errors);
            if (item.Use?.TechniqueId is { } techniqueId) Require(techniqueIds, techniqueId, item.Key, "TechniqueDefinition", errors);
            foreach (var effectId in item.PassiveEffectIds) Require(effectIds, effectId, item.Key, "EffectDefinition", errors);
            foreach (var effectId in item.Consumable?.EffectIds ?? []) Require(effectIds, effectId, item.Key, "EffectDefinition", errors);
            ValidateGenericReferences(item.Key, item.References.Values, allIds, errors);
        }

        foreach (var lootTable in LootTables ?? [])
        {
            foreach (var entry in lootTable.Entries)
            {
                Require(itemIds, entry.ItemId, lootTable.Key, "ItemDefinition", errors);
                foreach (var propertyId in entry.PropertyOverrides.Keys)
                    Require(propertyIds, propertyId, lootTable.Key, "ItemPropertyDefinition", errors);
            }
        }

        foreach (var resource in Resources ?? [])
        {
            foreach (var propertyId in resource.PropertyIds) Require(propertyIds, propertyId, resource.Key, "ItemPropertyDefinition", errors);
            if (resource.Harvest.LootTableId is { } lootId) Require(lootIds, lootId, resource.Key, "LootTableDefinition", errors);
            if (resource.Harvest.RequiredProfessionId is { } professionId)
                Require(professionIds, professionId, resource.Key, "ProfessionDefinition", errors);
            ValidateEventHooks(resource.Key, resource.EventHooks, eventIds, errors);
        }

        foreach (var mob in Mobs ?? [])
        {
            if (mob.LootTableId is { } lootId) Require(lootIds, lootId, mob.Key, "LootTableDefinition", errors);
            ValidateCombatReferences(mob.Key, mob.Combat, techniqueIds, effectIds, errors);
            ValidateEventHooks(mob.Key, mob.EventHooks, eventIds, errors);
        }

        foreach (var npc in Npcs ?? [])
        {
            if (npc.LootTableId is { } lootId) Require(lootIds, lootId, npc.Key, "LootTableDefinition", errors);
            if (npc.Combat is { } combat) ValidateCombatReferences(npc.Key, combat, techniqueIds, effectIds, errors);
            ValidateEventHooks(npc.Key, npc.EventHooks, eventIds, errors);
            ValidateGenericReferences(npc.Key, npc.Services.Values, allIds, errors);
        }

        foreach (var technique in Techniques ?? [])
        {
            ValidateEventHooks(technique.Key, technique.EventHooks, eventIds, errors);
            ValidateConditionGroup(technique.Key, technique.CastRequirements, allIds, errors);
            foreach (var action in technique.Actions)
                ValidateTechniqueAction(technique.Key, action, mapIds, spawnableIds, effectIds, eventIds, errors);
        }

        foreach (var effect in Effects ?? [])
        {
            foreach (var action in effect.OnApply.Concat(effect.OnTick).Concat(effect.OnExpire))
                ValidateTechniqueAction(effect.Key, action, mapIds, spawnableIds, effectIds, eventIds, errors);
        }

        foreach (var profession in Professions ?? [])
        {
            foreach (var techniqueId in profession.TechniqueIds) Require(techniqueIds, techniqueId, profession.Key, "TechniqueDefinition", errors);
            foreach (var activity in profession.Activities.Values)
            {
                foreach (var techniqueId in activity.TechniqueIds) Require(techniqueIds, techniqueId, profession.Key, "TechniqueDefinition", errors);
                ValidateConditionGroup(profession.Key, activity.Requirements, allIds, errors);
                ValidateEventHooks(profession.Key, activity.EventHooks, eventIds, errors);
            }
            foreach (var specialization in profession.Specializations.Values)
                foreach (var techniqueId in specialization.TechniqueIds)
                    Require(techniqueIds, techniqueId, profession.Key, "TechniqueDefinition", errors);
            ValidateEventHooks(profession.Key, profession.EventHooks, eventIds, errors);
        }

        foreach (var tradition in Traditions ?? [])
        {
            foreach (var expression in tradition.Expressions.Values)
                foreach (var techniqueId in expression.TechniqueIds)
                    Require(techniqueIds, techniqueId, tradition.Key, "TechniqueDefinition", errors);
            foreach (var unlock in tradition.TechniqueUnlocks)
            {
                Require(techniqueIds, unlock.TechniqueId, tradition.Key, "TechniqueDefinition", errors);
                ValidateConditionGroup(tradition.Key, unlock.Requirements, allIds, errors);
            }
            ValidateEventHooks(tradition.Key, tradition.EventHooks, eventIds, errors);
        }

        foreach (var recipe in Recipes ?? [])
        {
            Require(professionIds, recipe.ProfessionId, recipe.Key, "ProfessionDefinition", errors);
            foreach (var itemId in recipe.Ingredients.Keys) Require(itemIds, itemId, recipe.Key, "ItemDefinition", errors);
            foreach (var itemId in recipe.Outputs.Keys) Require(itemIds, itemId, recipe.Key, "ItemDefinition", errors);
            foreach (var propertyId in recipe.OutputPropertyRanges.Keys)
                Require(propertyIds, propertyId, recipe.Key, "ItemPropertyDefinition", errors);
            if (recipe.CompletionEventId is { } eventId) Require(eventIds, eventId, recipe.Key, "EventDefinition", errors);
            ValidateConditionGroup(recipe.Key, recipe.Requirements, allIds, errors);
        }

        foreach (var quest in Quests ?? [])
        {
            if (quest.StartEventId is { } start) Require(eventIds, start, quest.Key, "EventDefinition", errors);
            if (quest.EndEventId is { } end) Require(eventIds, end, quest.Key, "EventDefinition", errors);
            ValidateEventHooks(quest.Key, quest.EventHooks, eventIds, errors);
            ValidateConditionGroup(quest.Key, quest.Requirements, allIds, errors);
            foreach (var task in quest.Tasks)
            {
                if (task.CompletionEventId is { } completion) Require(eventIds, completion, quest.Key, "EventDefinition", errors);
                ValidateConditionGroup(quest.Key, task.CompletionConditions, allIds, errors);
                if (task.TargetDefinitionId is not { } target) continue;
                var expected = task.Objective switch
                {
                    QuestObjectiveKind.GatherItems or QuestObjectiveKind.UseItem => itemIds,
                    QuestObjectiveKind.KillMobs => mobIds,
                    QuestObjectiveKind.CraftItem => recipeIds,
                    QuestObjectiveKind.ReachProfessionMastery => professionIds,
                    _ => allIds
                };
                Require(expected, target, quest.Key, $"objetivo {task.Objective}", errors);
            }
        }

        foreach (var spawnTable in SpawnTables ?? [])
        {
            foreach (var entry in spawnTable.Entries)
            {
                var expected = entry.Kind switch
                {
                    SpawnEntityKind.Mob => mobIds,
                    SpawnEntityKind.Npc => npcIds,
                    SpawnEntityKind.Resource => resourceIds,
                    _ => allIds
                };
                Require(expected, entry.DefinitionId, spawnTable.Key, $"spawn {entry.Kind}", errors);
                ValidateConditionGroup(spawnTable.Key, entry.Conditions, allIds, errors);
            }
        }

        foreach (var dungeon in Dungeons ?? [])
        {
            foreach (var mapId in dungeon.MapIds) Require(mapIds, mapId, dungeon.Key, "MapDefinition", errors);
            if (dungeon.EntryEventId is { } entry) Require(eventIds, entry, dungeon.Key, "EventDefinition", errors);
            if (dungeon.CompletionEventId is { } completion) Require(eventIds, completion, dungeon.Key, "EventDefinition", errors);
            ValidateConditionGroup(dungeon.Key, dungeon.EntryRequirements, allIds, errors);
            foreach (var stage in dungeon.Stages)
            {
                Require(mapIds, stage.MapId, dungeon.Key, "MapDefinition", errors);
                foreach (var spawnId in stage.SpawnTableIds) Require(spawnTableIds, spawnId, dungeon.Key, "SpawnTableDefinition", errors);
                foreach (var eventId in stage.EventIds) Require(eventIds, eventId, dungeon.Key, "EventDefinition", errors);
                foreach (var bossId in stage.BossMobIds) Require(mobIds, bossId, dungeon.Key, "MobDefinition", errors);
                if (stage.CompletionEventId is { } stageEvent) Require(eventIds, stageEvent, dungeon.Key, "EventDefinition", errors);
                ValidateConditionGroup(dungeon.Key, stage.CompletionConditions, allIds, errors);
            }
        }

        foreach (var evt in Events ?? [])
        {
            if (evt.Placement is { } placement) Require(mapIds, placement.MapId, evt.Key, "MapDefinition", errors);
            foreach (var page in evt.Pages)
            {
                ValidateConditionGroup(evt.Key, page.Conditions, allIds, errors);
                foreach (var command in page.CommandLists.Values.SelectMany(static commands => commands))
                {
                    ValidateGenericReferences(evt.Key, command.References.Values, allIds, errors);
                    if (command.Condition is not null) ValidateConditionGroup(evt.Key, command.Condition, allIds, errors);
                }
            }
        }
    }

    private static void ValidateTechniqueAction(
        ContentKey owner,
        TechniqueActionDefinition action,
        HashSet<DefinitionId> mapIds,
        HashSet<DefinitionId> spawnableIds,
        HashSet<DefinitionId> effectIds,
        HashSet<DefinitionId> eventIds,
        List<string> errors)
    {
        if (action.EffectId is { } effectId) Require(effectIds, effectId, owner, "EffectDefinition", errors);
        if (action.EventId is { } eventId) Require(eventIds, eventId, owner, "EventDefinition", errors);
        if (action.SpawnDefinitionId is { } spawnId) Require(spawnableIds, spawnId, owner, "spawnable Definition", errors);
        if (action.DestinationMapId is { } mapId) Require(mapIds, mapId, owner, "MapDefinition", errors);
    }

    private static void ValidateCombatReferences(
        ContentKey owner,
        CreatureCombatDefinition combat,
        HashSet<DefinitionId> techniqueIds,
        HashSet<DefinitionId> effectIds,
        List<string> errors)
    {
        foreach (var techniqueId in combat.TechniqueIds) Require(techniqueIds, techniqueId, owner, "TechniqueDefinition", errors);
        foreach (var effectId in combat.ImmunityEffectIds) Require(effectIds, effectId, owner, "EffectDefinition", errors);
    }

    private static void ValidateEventHooks(
        ContentKey owner,
        IReadOnlyDictionary<string, DefinitionId> hooks,
        HashSet<DefinitionId> eventIds,
        List<string> errors)
    {
        foreach (var eventId in hooks.Values) Require(eventIds, eventId, owner, "EventDefinition", errors);
    }

    private static void ValidateConditionGroup(
        ContentKey owner,
        ConditionGroupDefinition group,
        HashSet<DefinitionId> allIds,
        List<string> errors)
    {
        foreach (var condition in group.Conditions)
            ValidateGenericReferences(owner, condition.References.Values, allIds, errors);
        foreach (var nested in group.Groups) ValidateConditionGroup(owner, nested, allIds, errors);
    }

    private static void ValidateGenericReferences(
        ContentKey owner,
        IEnumerable<DefinitionId> references,
        HashSet<DefinitionId> allIds,
        List<string> errors)
    {
        foreach (var reference in references) Require(allIds, reference, owner, "Definition", errors);
    }

    private static void Require(
        HashSet<DefinitionId> available,
        DefinitionId referenced,
        ContentKey owner,
        string expectedType,
        List<string> errors)
    {
        if (!available.Contains(referenced)) errors.Add($"{owner} referencia {expectedType} inexistente: {referenced}.");
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
            var package = JsonSerializer.Deserialize<ContentPackage>(json, JsonOptions)
                ?? throw new InvalidDataException("ContentPackage vacío.");
            return package with { Events = package.Events ?? [] };
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
            Dungeons: [], Quests: [], ItemProperties: [])
        {
            Events = []
        };
    }

    private void RegisterAllInto(DefinitionRegistry registry)
    {
        foreach (var definition in All()) registry.Register(definition);
    }
}
