using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class NpcEditorForm : DefinitionEditorForm
{
    public NpcEditorForm()
    {
        InitializeComponent();
    }

    public NpcEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Npcs)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not NpcDefinition npc) return;
        visualKeyTextBox.Text = npc.VisualKey.Value;
        combatEnabledCheck.Checked = npc.CombatEnabled;
        BindDefinitionCombo<LootTableDefinition>(lootTableCombo, npc.LootTableId);
        SelectEnum(lootModeCombo, npc.LootMode);
        aggressiveCheck.Checked = npc.Behavior.Aggressive;
        SelectEnum(movementCombo, npc.Behavior.Movement);
        SetNumeric(sightNumeric, (decimal)npc.Behavior.SightRange);
        SetNumeric(resetNumeric, (decimal)npc.Behavior.ResetRadius);
        var combat = npc.Combat ?? new CreatureCombatDefinition();
        SetNumeric(levelNumeric, combat.Level);
        SetNumeric(experienceNumeric, combat.Experience);
        SetNumeric(damageNumeric, (decimal)combat.BaseDamage);
        SelectEnum(elementCombo, combat.BasicAttackElement);
        SetNumeric(healthNumeric, (decimal)combat.MaxVitals.GetValueOrDefault(VitalId.Health));
        SetNumeric(manaNumeric, (decimal)combat.MaxVitals.GetValueOrDefault(VitalId.Mana));
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var npc = current as NpcDefinition ?? throw new InvalidOperationException("La selección no es un NPC.");
        CreatureCombatDefinition? combat = null;
        if (combatEnabledCheck.Checked)
        {
            var vitals = new Dictionary<VitalId, float>(npc.Combat?.MaxVitals ?? []);
            vitals[VitalId.Health] = (float)healthNumeric.Value;
            vitals[VitalId.Mana] = (float)manaNumeric.Value;
            var previous = npc.Combat ?? new CreatureCombatDefinition();
            combat = new CreatureCombatDefinition(
                (int)levelNumeric.Value,
                (long)experienceNumeric.Value,
                (float)damageNumeric.Value,
                ReadEnum(elementCombo, previous.BasicAttackElement),
                previous.CriticalChancePercent,
                previous.CriticalMultiplier,
                previous.Tenacity,
                previous.AttackIntervalMilliseconds,
                previous.TechniqueIntervalMilliseconds,
                previous.Stats,
                vitals,
                previous.VitalRegeneration,
                previous.Scaling,
                previous.TechniqueIds,
                previous.ImmunityEffectIds,
                previous.Parameters);
        }

        return new NpcDefinition(
            id, key, name, description, enabled, version, tags,
            ReadContentKey(visualKeyTextBox),
            combatEnabledCheck.Checked,
            new CreatureBehaviorDefinition(
                aggressiveCheck.Checked,
                npc.Behavior.AttackAllies,
                npc.Behavior.Swarm,
                npc.Behavior.FleeHealthPercentage,
                npc.Behavior.TargetPriority,
                ReadEnum(movementCombo, npc.Behavior.Movement),
                (float)sightNumeric.Value,
                (float)resetNumeric.Value,
                npc.Behavior.NpcVsNpcEnabled),
            combat,
            ReadDefinitionId(lootTableCombo),
            ReadEnum(lootModeCombo, npc.LootMode),
            npc.Services,
            npc.EventHooks,
            npc.Metadata);
    }
}
