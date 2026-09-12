using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

/// <summary>
/// Resultado inmutable de una derrota autoritativa. El WorldRuntime consume Loot para materializarlo
/// en el mundo; ProgressionSystem ya aplicó la experiencia antes de publicar el resultado.
/// </summary>
public sealed record DefeatResolution(
    LivingEntity? Attacker,
    LivingEntity Defeated,
    Player? RewardPlayer,
    long ExperienceGranted,
    IReadOnlyList<LootRoll> Loot,
    long OccurredAtMilliseconds);

/// <summary>
/// Único dueño de recompensas por derrota. Evita que handlers concretos otorguen XP/loot y hace que
/// daño directo, casts, channels, efectos, proyectiles y zonas compartan exactamente el mismo cierre.
/// </summary>
public sealed class DefeatSystem
{
    private readonly DefinitionRegistry definitions;
    private readonly ProgressionSystem progression;
    private readonly LootSystem loot;

    public DefeatSystem(DefinitionRegistry definitions, ProgressionSystem progression, LootSystem loot)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
        this.loot = loot ?? throw new ArgumentNullException(nameof(loot));
    }

    public DefeatResolution Resolve(CombatDefeatEvent defeat)
    {
        ArgumentNullException.ThrowIfNull(defeat);
        Player? rewardPlayer = null;
        long experience = 0;
        IReadOnlyList<LootRoll> rolledLoot = [];

        if (defeat.Attacker is Player player && defeat.Target is Mob mob)
        {
            rewardPlayer = player;
            if (mob.ExperienceReward > 0)
            {
                progression.GrantExperience(player, mob.ExperienceReward);
                experience = mob.ExperienceReward;
            }

            if (definitions.TryGet<MobDefinition>(mob.DefinitionId, out var definition) && definition is not null)
                rolledLoot = loot.Roll(definition);

            player.MarkDirty();
        }

        if (defeat.Target is Player defeatedPlayer) defeatedPlayer.MarkDirty();

        return new DefeatResolution(
            defeat.Attacker,
            defeat.Target,
            rewardPlayer,
            experience,
            rolledLoot,
            defeat.OccurredAtMilliseconds);
    }
}
