using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;

internal static class ProgressionCombatVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        Check(ProgressionRules.EarnedAttributePoints(100) == 297, "Progression: Lv100 acumula 297 puntos");
        Check(ProgressionRules.CostForNextPoint(100) == 1, "Progression: 100→101 cuesta 1");
        Check(ProgressionRules.CostForNextPoint(101) == 2, "Progression: 101→102 cuesta 2");
        Check(ProgressionRules.CostForNextPoint(200) == 3, "Progression: 200→201 cuesta 3");

        var table = LevelProgressionDefinition.CreateV01Seed();
        Check(table.ExperienceToNextLevel(1) == 100, "Progression: XP Lv1");
        Check(table.ExperienceToNextLevel(5) == 1423, "Progression: XP Lv5");
        Check(table.ExperienceToNextLevel(50) == 63577, "Progression: XP Lv50");
        Check(table.ExperienceToNextLevel(99) == 196245, "Progression: XP Lv99");

        var level100 = ProgressionRules.CreateInitial() with
        {
            Level = 100,
            AvailableAttributePoints = 297
        };
        ProgressionRules.Validate(level100);
        var allIn = ProgressionRules.Allocate(level100, PrimaryAttributeId.Strength, 192);
        Check(allIn.NaturalAttributes.Strength == 202 && allIn.AvailableAttributePoints == 2,
            "Progression: all-in Lv100 alcanza 202 naturales y sobran 2");

        var player = new Player(
            new EntityId(10),
            new AccountId(Guid.NewGuid()),
            new CharacterId(Guid.NewGuid()),
            new MapInstanceId(1),
            new Vector2Data(10, 10),
            new ContentKey("template.player"),
            "Tester");
        Check(player.Stats.Primary.Strength == 10 && player.Stats.Primary.Intelligence == 10 &&
              player.Stats.Primary.Agility == 10 && player.Stats.Primary.Spirit == 10 &&
              player.Stats.Primary.Vitality == 10,
            "Progression: jugador nuevo inicia 10/10/10/10/10");
        Check(player.MaxHealth == 430 && player.MaxMana == 162,
            "Progression: jugador Lv1 usa HP/PM derivados V0.1");

        player.TakeDamage(30);
        player.TryConsumeMana(20);
        var progression = new ProgressionSystem(table);
        progression.GrantExperience(player, 100);
        Check(player.Level == 2 && player.AttributePoints == 3,
            "Progression: XP sube nivel y entrega 3 puntos");
        Check(player.MaxHealth == 440 && player.Health == 410 && player.MaxMana == 164 && player.Mana == 144,
            "Progression: subir nivel preserva déficit de HP/PM");
        progression.Allocate(player, PrimaryAttributeId.Vitality);
        Check(player.Stats.Primary.Vitality == 11 && player.AttributePoints == 2 &&
              player.MaxHealth == 452 && player.Health == 422,
            "Progression: asignar VIT recalcula HP sin curación completa");

        var fire = new AttributeDamageFormula(Element.Fire, PrimaryAttributeId.Intelligence, 100, 1);
        var raw = CanonicalDamageRules.CalculateRaw(fire, player.Stats.Primary);
        Check(Math.Abs(raw - 110f) < 0.001f, "Combat: Base100 + INT10 produce 110 raw");
        Check(Math.Abs(CanonicalDamageRules.ApplyPveResistance(110, 50) - 55f) < 0.001f,
            "Combat: resistencia 50% mitiga a 55");
        Check(Math.Abs(CanonicalDamageRules.ApplyPveResistance(110, 95) - 22f) < 0.001f,
            "Combat: cap PvE V0.1 de 80%");
        Check(Math.Abs(CanonicalDamageRules.ApplyPveResistance(110, -20) - 132f) < 0.001f,
            "Combat: resistencia negativa crea vulnerabilidad");

        VerifyDamagePipeline();

        var dummy = TrainingDummyFixture.CreateEntity(
            new EntityId(20),
            new MapInstanceId(1),
            new Vector2Data(12, 10));
        Check(dummy.Level == 10 && dummy.MaxHealth == 50_000 && dummy.Immortal &&
              dummy.Behavior.Movement == CreatureMovementMode.Stationary && dummy.ExperienceReward == 0,
            "Dummy: es Mob real estacionario Lv10/50k/0XP/inmortal");

        var combat = new CombatSystem();
        var hit = combat.ExecuteAttributeDamage(player, dummy, TrainingDummyFixture.EarthTechnique(), 1_000);
        Check(hit.AppliedDamage == 110 && dummy.Health == 49_890,
            "Dummy: golpe Tierra/STR atraviesa CombatSystem real");
        var meter = combat.Telemetry.Snapshot(dummy.Id, 1_000);
        Check(meter is not null && meter.LastAppliedDamage == 110 && meter.Hits == 1 && meter.TotalDamage == 110,
            "Dummy: telemetría registra último golpe/hits/total");

        combat.ApplyDamage(player, dummy, 100_000, Element.Neutral, nowMilliseconds: 2_000);
        Check(dummy.Health == 1 && dummy.IsAlive,
            "Dummy: inmortalidad es flag de LivingEntity y usa el mismo pipeline");
    }

    private static void VerifyDamagePipeline()
    {
        var breakdown = DamagePipeline.Resolve(new DamageCalculationInput(
            Element.Fire,
            BaseDamage: 100,
            Characteristic: 200,
            CharacteristicScale: 1,
            Power: 50,
            FlatDamage: 25,
            CriticalMultiplier: 1.5f,
            HardDefense: 100,
            SoftDefense: 30,
            FlatReduction: 10,
            ResistancePercent: 25,
            FinalMultiplier: 1.2f));

        Check(breakdown.AfterCharacteristicAndPower == 350,
            "Combat pipeline: característica + Power escalan la base estilo Dofus");
        Check(breakdown.AfterFlatDamage == 375 && breakdown.AfterCritical == 562,
            "Combat pipeline: daño plano y crítico son etapas explícitas");
        Check(Math.Abs(breakdown.HardDefenseMultiplier - .82f) < .0001f && breakdown.AfterHardDefense == 460,
            "Combat pipeline: Hard DEF usa curva multiplicativa estilo RO");
        Check(breakdown.AfterSoftDefense == 430 && breakdown.AfterFlatReduction == 420,
            "Combat pipeline: Soft DEF y reducción plana son sustractivas");
        Check(breakdown.AfterResistance == 315 && breakdown.FinalDamage == 378,
            "Combat pipeline: resistencia y multiplicador final cierran el cálculo");

        var capped = DamagePipeline.Resolve(new DamageCalculationInput(
            Element.Earth, 100, 0, ResistancePercent: 95, UsePositivePveResistanceCap: true));
        Check(capped.EffectiveResistancePercent == 80 && capped.FinalDamage == 20,
            "Combat pipeline: cap PvE positivo permanece centralizado");

        var vulnerable = DamagePipeline.Resolve(new DamageCalculationInput(
            Element.Water, 100, 0, ResistancePercent: -20));
        Check(vulnerable.FinalDamage == 120,
            "Combat pipeline: resistencia negativa conserva vulnerabilidad");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
