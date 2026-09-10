using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server;

/// <summary>
/// Fixture exclusivo de Development/Test para medir progresión y combate.
/// Usa MobDefinition/Mob reales; no define un receptor de daño alterno.
/// </summary>
public static class TrainingDummyFixture
{
    public static readonly DefinitionId DefinitionId = DefinitionId.From(Guid.Parse("7a9b6a42-ec70-4d6f-ae3d-44d6044129a1"));
    public static readonly ContentKey DefinitionKey = new("fixture.training_dummy");
    public static readonly ContentKey VisualKey = new("template.player");

    public static MobDefinition CreateDefinition()
        => new(
            DefinitionId,
            DefinitionKey,
            "Muñeco de entrenamiento",
            "Fixture técnico para medir fórmulas de combate.",
            enabled: true,
            version: 1,
            tags: ["fixture", "training", "development"],
            visualKey: VisualKey,
            lootTableId: null,
            lootMode: LootMode.Shared,
            behavior: new CreatureBehaviorDefinition(
                aggressive: false,
                attackAllies: false,
                swarm: false,
                fleeHealthPercentage: 0,
                targetPriority: CreatureTargetPriority.Nearest,
                movement: CreatureMovementMode.Stationary,
                sightRange: 0,
                resetRadius: 0,
                npcVsNpcEnabled: false),
            combat: new CreatureCombatDefinition(
                level: 10,
                experience: 0,
                baseDamage: 0,
                basicAttackElement: Element.Neutral,
                criticalChancePercent: 0,
                criticalMultiplier: 1.5f,
                tenacity: 0,
                attackIntervalMilliseconds: 0,
                techniqueIntervalMilliseconds: 0,
                stats: new Dictionary<StatId, float>
                {
                    [StatId.ResistEarth] = 0,
                    [StatId.ResistFire] = 0,
                    [StatId.ResistAir] = 0,
                    [StatId.ResistWater] = 0,
                    [StatId.ResistNeutral] = 0
                },
                maxVitals: new Dictionary<VitalId, float>
                {
                    [VitalId.Health] = 50_000,
                    [VitalId.Mana] = 0
                },
                parameters: new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                {
                    ["track_damage_telemetry"] = 1,
                    ["training_dummy"] = 1
                }));

    public static Mob CreateEntity(EntityId id, MapInstanceId instance, Vector2Data position)
    {
        var mob = new Mob(id, CreateDefinition(), instance, position);
        mob.SetImmortal(true);
        return mob;
    }

    public static AttributeDamageFormula EarthTechnique(float scaling = 1f)
        => new(Element.Earth, PrimaryAttributeId.Strength, 100f, scaling);

    public static AttributeDamageFormula FireTechnique(float scaling = 1f)
        => new(Element.Fire, PrimaryAttributeId.Intelligence, 100f, scaling);

    public static AttributeDamageFormula AirTechnique(float scaling = 1f)
        => new(Element.Air, PrimaryAttributeId.Agility, 100f, scaling);

    public static AttributeDamageFormula WaterTechnique(float scaling = 1f)
        => new(Element.Water, PrimaryAttributeId.Spirit, 100f, scaling);

    public static AttributeDamageFormula NeutralStrengthTechnique(float scaling = 1f)
        => new(Element.Neutral, PrimaryAttributeId.Strength, 100f, scaling);
}
