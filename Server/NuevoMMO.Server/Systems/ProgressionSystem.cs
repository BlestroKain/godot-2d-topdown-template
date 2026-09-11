using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

/// <summary>
/// Autoridad de nivel, XP, distribución natural y recálculo de stats del jugador.
/// La entidad conserva estado; este sistema decide cómo cambia.
/// </summary>
public sealed class ProgressionSystem
{
    private readonly LevelProgressionDefinition progression;
    private readonly EquipmentSystem? equipment;

    public ProgressionSystem(LevelProgressionDefinition? progression = null, EquipmentSystem? equipment = null)
    {
        this.progression = progression ?? LevelProgressionDefinition.CreateV01Seed();
        this.progression.Validate();
        this.equipment = equipment;
    }

    public LevelProgressionDefinition Definition => progression;

    public void Initialize(Player player, PlayerProgressionState? state = null, bool preserveVitals = false)
    {
        ArgumentNullException.ThrowIfNull(player);
        var resolved = state ?? ProgressionRules.CreateInitial();
        ProgressionRules.Validate(resolved);
        player.SetProgression(resolved);
        Recalculate(player, preserveVitals);
    }

    public PlayerProgressionState GrantExperience(Player player, long amount)
    {
        ArgumentNullException.ThrowIfNull(player);
        var state = ProgressionRules.GainExperience(player.Progression, amount, progression);
        player.SetProgression(state);
        Recalculate(player, preserveVitals: true);
        return state;
    }

    public PlayerProgressionState Allocate(Player player, PrimaryAttributeId attribute, int increments = 1)
    {
        ArgumentNullException.ThrowIfNull(player);
        var state = ProgressionRules.Allocate(player.Progression, attribute, increments);
        player.SetProgression(state);
        Recalculate(player, preserveVitals: true);
        return state;
    }

    public void Recalculate(Player player, bool preserveVitals = true)
    {
        ArgumentNullException.ThrowIfNull(player);
        var natural = player.Progression.NaturalAttributes.ToPrimaryStats();
        var flat = equipment?.FlatStatBonuses(player) ?? new Dictionary<StatId, float>();
        var percent = equipment?.PercentStatBonuses(player) ?? new Dictionary<StatId, float>();

        player.Stats.Primary.Strength = ResolvePrimary(natural.Strength, StatId.Strength, flat, percent);
        player.Stats.Primary.Intelligence = ResolvePrimary(natural.Intelligence, StatId.Intelligence, flat, percent);
        player.Stats.Primary.Agility = ResolvePrimary(natural.Agility, StatId.Agility, flat, percent);
        player.Stats.Primary.Spirit = ResolvePrimary(natural.Spirit, StatId.Spirit, flat, percent);
        player.Stats.Primary.Vitality = ResolvePrimary(natural.Vitality, StatId.Vitality, flat, percent);

        var stats = CanonicalStatCalculator.Calculate(player.Level, player.Stats.Primary);
        if (preserveVitals)
            player.SetMaximumVitalsPreservingDeficit(stats.MaxHealth, stats.MaxMana);
        else
            player.SetMaximumVitals(stats.MaxHealth, stats.MaxMana, refill: true);
    }

    private static int ResolvePrimary(
        int natural,
        StatId id,
        IReadOnlyDictionary<StatId, float> flat,
        IReadOnlyDictionary<StatId, float> percent)
    {
        var flatValue = flat.GetValueOrDefault(id);
        var percentValue = percent.GetValueOrDefault(id);
        if (!float.IsFinite(flatValue) || !float.IsFinite(percentValue))
            throw new InvalidOperationException($"Bono no finito para {id}.");

        var value = (natural + flatValue) * (1f + percentValue / 100f);
        if (!float.IsFinite(value) || value < 0 || value > int.MaxValue)
            throw new OverflowException($"Stat efectivo fuera de rango: {id}.");
        return (int)MathF.Round(value, MidpointRounding.AwayFromZero);
    }
}
