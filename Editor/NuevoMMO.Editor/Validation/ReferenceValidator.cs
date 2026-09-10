using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class ReferenceValidator(DefinitionRegistry registry)
{
    public IReadOnlyList<string> Validate(MobDefinition mob)
    {
        var errors = new List<string>();
        if (mob.LootTableId is { } loot && !registry.TryGet<LootTableDefinition>(loot, out _))
            errors.Add($"LootTable inexistente: {loot.Value}.");
        return errors;
    }
}
