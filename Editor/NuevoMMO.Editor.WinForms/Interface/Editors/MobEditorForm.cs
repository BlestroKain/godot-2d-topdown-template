using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class MobEditorForm : DefinitionEditorForm
{
    public MobEditorForm()
    {
        InitializeComponent();
    }

    public MobEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Mobs)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not MobDefinition mob) return;
        visualKeyTextBox.Text = mob.VisualKey.Value;
        BindDefinitionCombo<LootTableDefinition>(lootTableCombo, mob.LootTableId);
        SelectEnum(lootModeCombo, mob.LootMode);
        aggressiveCheck.Checked = mob.Behavior.Aggressive;
        attackAlliesCheck.Checked = mob.Behavior.AttackAllies;
        swarmCheck.Checked = mob.Behavior.Swarm;
        npcVsNpcCheck.Checked = mob.Behavior.NpcVsNpcEnabled;
        SelectEnum(movementCombo, mob.Behavior.Movement);
        SelectEnum(targetPriorityCombo, mob.Behavior.TargetPriority);
        SetNumeric(fleeNumeric, mob.Behavior.FleeHealthPercentage);
        SetNumeric(sightNumeric, (decimal)mob.Behavior.SightRange);
        SetNumeric(resetNumeric, (decimal)mob.Behavior.ResetRadius);
        SetNumeric(levelNumeric, mob.Combat.Level);
        SetNumeric(experienceNumeric, mob.Combat.Experience);
        SetNumeric(damageNumeric, (decimal)mob.Combat.BaseDamage);
        SelectEnum(elementCombo, mob.Combat.BasicAttackElement);
        SetNumeric(critChanceNumeric, (decimal)mob.Combat.CriticalChancePercent);
        SetNumeric(critMultiplierNumeric, (decimal)mob.Combat.CriticalMultiplier);
        SetNumeric(tenacityNumeric, (decimal)mob.Combat.Tenacity);
        SetNumeric(attackIntervalNumeric, mob.Combat.AttackIntervalMilliseconds);
        SetNumeric(healthNumeric, (decimal)mob.Combat.MaxVitals.GetValueOrDefault(VitalId.Health));
        SetNumeric(manaNumeric, (decimal)mob.Combat.MaxVitals.GetValueOrDefault(VitalId.Mana));
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var mob = current as MobDefinition ?? throw new InvalidOperationException("La selección no es un Mob.");
        var vitals = new Dictionary<VitalId, float>(mob.Combat.MaxVitals);
        vitals[VitalId.Health] = (float)healthNumeric.Value;
        vitals[VitalId.Mana] = (float)manaNumeric.Value;
        return new MobDefinition(
            id, key, name, description, enabled, version, tags,
            ReadContentKey(visualKeyTextBox),
            ReadDefinitionId(lootTableCombo),
            ReadEnum(lootModeCombo, mob.LootMode),
            new CreatureBehaviorDefinition(
                aggressiveCheck.Checked,
                attackAlliesCheck.Checked,
                swarmCheck.Checked,
                (byte)fleeNumeric.Value,
                ReadEnum(targetPriorityCombo, mob.Behavior.TargetPriority),
                ReadEnum(movementCombo, mob.Behavior.Movement),
                (float)sightNumeric.Value,
                (float)resetNumeric.Value,
                npcVsNpcCheck.Checked),
            new CreatureCombatDefinition(
                (int)levelNumeric.Value,
                (long)experienceNumeric.Value,
                (float)damageNumeric.Value,
                ReadEnum(elementCombo, mob.Combat.BasicAttackElement),
                (float)critChanceNumeric.Value,
                (float)critMultiplierNumeric.Value,
                (float)tenacityNumeric.Value,
                (int)attackIntervalNumeric.Value,
                mob.Combat.TechniqueIntervalMilliseconds,
                mob.Combat.Stats,
                vitals,
                mob.Combat.VitalRegeneration,
                mob.Combat.Scaling,
                mob.Combat.TechniqueIds,
                mob.Combat.ImmunityEffectIds,
                mob.Combat.Parameters),
            mob.EventHooks,
            mob.Metadata);
    }
}
