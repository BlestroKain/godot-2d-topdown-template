using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

/// <summary>Único adaptador servidor -> protocolo para stats visibles del jugador.</summary>
public static class PlayerStatsProjection
{
    public static PlayerStatsPacket Create(Player player, ProgressionSystem progression)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(progression);

        var natural = player.Progression.NaturalAttributes;
        var effective = player.Stats.Primary;
        var derived = CanonicalStatCalculator.Calculate(player.Level, effective);
        var experienceToNext = player.Level >= progression.Definition.ReferenceLevelCap
            ? 0
            : progression.Definition.ExperienceToNextLevel(player.Level);

        return new PlayerStatsPacket(new PlayerStatsSnapshot(
            player.Level,
            player.Experience,
            experienceToNext,
            player.AttributePoints,
            Attribute(natural.Strength, effective.Strength),
            Attribute(natural.Intelligence, effective.Intelligence),
            Attribute(natural.Agility, effective.Agility),
            Attribute(natural.Spirit, effective.Spirit),
            Attribute(natural.Vitality, effective.Vitality),
            player.Health,
            player.MaxHealth,
            player.Mana,
            player.MaxMana,
            derived.Defense,
            derived.ManaRegenPerSecond,
            derived.OutOfCombatManaRegenPerSecond,
            player.Stats.Secondary.Luck,
            player.Stats.Resistances.Earth,
            player.Stats.Resistances.Fire,
            player.Stats.Resistances.Air,
            player.Stats.Resistances.Water,
            player.Stats.Resistances.Neutral));
    }

    private static AttributeSnapshot Attribute(int natural, int effective)
        => new(natural, effective,
            natural >= ProgressionRules.MaximumNaturalAttribute ? 0 : ProgressionRules.CostForNextPoint(natural));
}
