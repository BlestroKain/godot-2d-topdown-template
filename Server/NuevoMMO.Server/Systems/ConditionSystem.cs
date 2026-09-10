using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public sealed record ConditionEvaluationContext(
    DefinitionId? MapDefinitionId = null,
    IReadOnlySet<string>? Regions = null,
    IReadOnlyDictionary<string, float>? Variables = null,
    IReadOnlyDictionary<string, bool>? Switches = null,
    IReadOnlySet<string>? AccountFlags = null,
    IReadOnlySet<string>? CharacterFlags = null,
    DateTimeOffset? Time = null);

/// <summary>
/// Evaluador común de ConditionDefinition para técnicas, eventos, quests e interacciones.
/// Las condiciones que dependen de sistemas aún no implementados pueden delegarse a CustomEvaluator.
/// </summary>
public sealed class ConditionSystem
{
    private readonly Func<Player, ConditionDefinition, ConditionEvaluationContext?, bool>? customEvaluator;
    private readonly Func<float> randomUnit;

    public ConditionSystem(
        Func<Player, ConditionDefinition, ConditionEvaluationContext?, bool>? customEvaluator = null,
        Func<float>? randomUnit = null)
    {
        this.customEvaluator = customEvaluator;
        this.randomUnit = randomUnit ?? Random.Shared.NextSingle;
    }

    public bool Evaluate(Player player, ConditionGroupDefinition group, ConditionEvaluationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(group);

        var results = group.Conditions.Select(condition => Evaluate(player, condition, context))
            .Concat(group.Groups.Select(child => Evaluate(player, child, context)))
            .ToArray();

        return group.Mode switch
        {
            ConditionGroupMode.All => results.All(static result => result),
            ConditionGroupMode.Any => results.Length > 0 && results.Any(static result => result),
            ConditionGroupMode.None => results.All(static result => !result),
            _ => throw new ArgumentOutOfRangeException(nameof(group.Mode), group.Mode, null)
        };
    }

    public bool Evaluate(Player player, ConditionDefinition condition, ConditionEvaluationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(condition);

        var value = condition.Kind switch
        {
            ConditionKind.Always => true,
            ConditionKind.HasItem => HasItem(player, condition),
            ConditionKind.ItemEquipped => ItemEquipped(player, condition),
            ConditionKind.HasTechnique => HasTechnique(player, condition),
            ConditionKind.ProfessionMastery => ProfessionMastery(player, condition),
            ConditionKind.CharacterLevel => CompareNumber(player.Level, ExpectedNumber(condition, "value", "level"), condition.Comparison),
            ConditionKind.Stat => CompareNumber(ReadStat(player.Stats, RequiredText(condition, "stat")),
                ExpectedNumber(condition, "value", "minimum"), condition.Comparison),
            ConditionKind.Map => MapMatches(condition, context),
            ConditionKind.Region => RegionMatches(condition, context),
            ConditionKind.Time => TimeMatches(condition, context),
            ConditionKind.RandomChance => RandomChance(condition),
            ConditionKind.EntityState => EntityState(player, condition),
            ConditionKind.Variable => VariableMatches(condition, context),
            ConditionKind.Switch => SwitchMatches(condition, context),
            ConditionKind.AccountFlag => FlagMatches(condition, context?.AccountFlags),
            ConditionKind.CharacterFlag => FlagMatches(condition, context?.CharacterFlags),
            _ => customEvaluator?.Invoke(player, condition, context) ?? false
        };

        return condition.Negate ? !value : value;
    }

    private static bool HasItem(Player player, ConditionDefinition condition)
    {
        var itemId = RequiredReference(condition, "item");
        var quantity = Math.Max(1, (int)MathF.Ceiling(OptionalNumber(condition, 1, "quantity", "value")));
        return CompareNumber(player.Inventory.QuantityOf(itemId), quantity, NormalizeExistence(condition.Comparison));
    }

    private static bool ItemEquipped(Player player, ConditionDefinition condition)
    {
        var itemDefinitionId = RequiredReference(condition, "item");
        var found = player.Equipment.Entries.Values.Any(itemId =>
            player.Inventory.TryGet(itemId, out var item) && item?.DefinitionId == itemDefinitionId);
        return CompareBool(found, condition.Comparison);
    }

    private static bool HasTechnique(Player player, ConditionDefinition condition)
    {
        var techniqueId = RequiredReference(condition, "technique");
        return CompareBool(player.Techniques.Knows(techniqueId), condition.Comparison);
    }

    private static bool ProfessionMastery(Player player, ConditionDefinition condition)
    {
        var professionId = RequiredReference(condition, "profession");
        if (!player.Professions.TryGet(professionId, out var progress) || progress is null) return false;

        if (condition.Text.TryGetValue("dimension", out var dimension) && !string.IsNullOrWhiteSpace(dimension))
        {
            var mastery = progress.Mastery.TryGetValue(dimension.Trim(), out var value) ? value : 0;
            return CompareNumber(mastery, ExpectedNumber(condition, "value", "minimum"), condition.Comparison);
        }

        var expectedLevel = OptionalNumber(condition, 1, "level", "value", "minimum");
        return CompareNumber(progress.Level, expectedLevel, condition.Comparison);
    }

    private static bool MapMatches(ConditionDefinition condition, ConditionEvaluationContext? context)
    {
        if (context?.MapDefinitionId is not { } mapId) return false;
        var expected = RequiredReference(condition, "map");
        return CompareBool(mapId == expected, condition.Comparison);
    }

    private static bool RegionMatches(ConditionDefinition condition, ConditionEvaluationContext? context)
    {
        if (context?.Regions is null) return false;
        var region = RequiredText(condition, "region");
        var found = context.Regions.Contains(region);
        return CompareBool(found, condition.Comparison);
    }

    private static bool TimeMatches(ConditionDefinition condition, ConditionEvaluationContext? context)
    {
        var time = context?.Time ?? DateTimeOffset.UtcNow;
        if (condition.Numbers.TryGetValue("hour", out var hour))
            return CompareNumber(time.Hour, hour, condition.Comparison);
        if (condition.Numbers.TryGetValue("dayOfWeek", out var day))
            return CompareNumber((int)time.DayOfWeek, day, condition.Comparison);
        return false;
    }

    private bool RandomChance(ConditionDefinition condition)
    {
        var chance = OptionalNumber(condition, 100, "chancePercent", "chance", "value");
        if (!float.IsFinite(chance) || chance is < 0 or > 100)
            throw new InvalidOperationException("RandomChance requiere chancePercent entre 0 y 100.");
        var roll = randomUnit();
        if (!float.IsFinite(roll) || roll is < 0 or >= 1)
            throw new InvalidOperationException("El generador aleatorio de condiciones debe producir [0,1).");
        return roll < chance / 100f;
    }

    private static bool EntityState(Player player, ConditionDefinition condition)
    {
        var state = RequiredText(condition, "state");
        var actual = state.ToLowerInvariant() switch
        {
            "alive" => player.IsAlive,
            "dead" => !player.IsAlive,
            "incombat" => player.CombatState.InCombat,
            "canmove" => player.MovementState.CanMove,
            _ => false
        };
        return CompareBool(actual, condition.Comparison);
    }

    private static bool VariableMatches(ConditionDefinition condition, ConditionEvaluationContext? context)
    {
        if (context?.Variables is null) return false;
        var key = RequiredText(condition, "key");
        if (!context.Variables.TryGetValue(key, out var actual))
            return condition.Comparison == ComparisonOperator.NotEqual;
        return CompareNumber(actual, ExpectedNumber(condition, "value"), condition.Comparison);
    }

    private static bool SwitchMatches(ConditionDefinition condition, ConditionEvaluationContext? context)
    {
        if (context?.Switches is null) return false;
        var key = RequiredText(condition, "key");
        var found = context.Switches.TryGetValue(key, out var actual) && actual;
        return CompareBool(found, condition.Comparison);
    }

    private static bool FlagMatches(ConditionDefinition condition, IReadOnlySet<string>? flags)
    {
        if (flags is null) return false;
        var key = RequiredText(condition, "flag", "key");
        return CompareBool(flags.Contains(key), condition.Comparison);
    }

    private static DefinitionId RequiredReference(ConditionDefinition condition, string key)
        => condition.References.TryGetValue(key, out var value) && !value.IsEmpty
            ? value
            : throw new InvalidOperationException($"Condition {condition.Kind} requiere reference '{key}'.");

    private static string RequiredText(ConditionDefinition condition, params string[] keys)
    {
        foreach (var key in keys)
            if (condition.Text.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
        throw new InvalidOperationException($"Condition {condition.Kind} requiere text '{string.Join("/", keys)}'.");
    }

    private static float ExpectedNumber(ConditionDefinition condition, params string[] keys)
    {
        foreach (var key in keys)
            if (condition.Numbers.TryGetValue(key, out var value)) return value;
        throw new InvalidOperationException($"Condition {condition.Kind} requiere number '{string.Join("/", keys)}'.");
    }

    private static float OptionalNumber(ConditionDefinition condition, float fallback, params string[] keys)
    {
        foreach (var key in keys)
            if (condition.Numbers.TryGetValue(key, out var value)) return value;
        return fallback;
    }

    private static ComparisonOperator NormalizeExistence(ComparisonOperator comparison)
        => comparison == ComparisonOperator.Exists ? ComparisonOperator.GreaterOrEqual : comparison;

    private static bool CompareNumber(float actual, float expected, ComparisonOperator comparison)
        => comparison switch
        {
            ComparisonOperator.Equal => MathF.Abs(actual - expected) <= .0001f,
            ComparisonOperator.NotEqual => MathF.Abs(actual - expected) > .0001f,
            ComparisonOperator.Greater => actual > expected,
            ComparisonOperator.GreaterOrEqual => actual >= expected,
            ComparisonOperator.Less => actual < expected,
            ComparisonOperator.LessOrEqual => actual <= expected,
            ComparisonOperator.Exists => true,
            _ => false
        };

    private static bool CompareBool(bool actual, ComparisonOperator comparison)
        => comparison switch
        {
            ComparisonOperator.Equal or ComparisonOperator.Exists => actual,
            ComparisonOperator.NotEqual => !actual,
            _ => actual
        };

    private static float ReadStat(StatBlock stats, string statName)
    {
        if (!Enum.TryParse<StatId>(statName, true, out var stat))
            throw new InvalidOperationException($"StatId desconocido: {statName}.");
        return stat switch
        {
            StatId.Strength => stats.Primary.Strength,
            StatId.Intelligence => stats.Primary.Intelligence,
            StatId.Agility => stats.Primary.Agility,
            StatId.Spirit => stats.Primary.Spirit,
            StatId.Vitality => stats.Primary.Vitality,
            StatId.Luck => stats.Secondary.Luck,
            StatId.ResistEarth => stats.Resistances.Earth,
            StatId.ResistFire => stats.Resistances.Fire,
            StatId.ResistAir => stats.Resistances.Air,
            StatId.ResistWater => stats.Resistances.Water,
            StatId.ResistNeutral => stats.Resistances.Neutral,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };
    }
}
